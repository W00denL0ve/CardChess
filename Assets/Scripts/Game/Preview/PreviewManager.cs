using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 预览管理器 — 全局编排器
/// 职责：栈管理、输入事件分发、公共视觉（遮罩/三角尖/预选框）、悬停调度
/// 具体预览逻辑委托给 PreviewBehaviour 子类
/// </summary>
public class PreviewManager : MonoBehaviour
{
    public static PreviewManager Instance { get; private set; }

    [Header("Visualizers")]
    [SerializeField] private GridVisualizer gridVisualizer;
    [SerializeField] private UnitVisualizer unitVisualizer;
    [SerializeField] private PathRenderer pathRenderer;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject markerPrefab;   // 三角尖
    [SerializeField] private GameObject boxPrefab;      // 预选框

    [Header("Settings")]
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private float markerZOffset = 0.3f;
    [SerializeField] private float boxHeightOffset = 0.1f;

    // ====================================================================
    //  状态
    // ====================================================================

    private PreviewState currentState = PreviewState.Idle;
    private Stack<PreviewStep> previewStack = new Stack<PreviewStep>();
    private PreviewStep CurrentStep => previewStack.Count > 0 ? previewStack.Peek() : null;

    // 行为注册表
    private Dictionary<PreviewType, PreviewBehaviour> behaviours = new Dictionary<PreviewType, PreviewBehaviour>();

    // 公共视觉
    private GameObject marker;
    private Animator markerAnimator;
    private GameObject box;
    private Animator boxAnimator;
    private GameObject previewMask;

    // 悬停
    private Vector2Int? lastHoveredCell;
    private Unit lastHoveredUnit;

    // 动画去抖
    private Coroutine markerRoutine;
    private Coroutine boxRoutine;

    // ====================================================================
    //  生命周期
    // ====================================================================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("PreviewManager: 检测到重复实例，销毁自身");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ResolveReferences();
        RegisterBehaviours();
        InitializeVisuals();
        SubscribeInput();
        Logger.Log("PreviewManager: 初始化完成");
    }

    void OnDestroy()
    {
        UnsubscribeInput();
    }

    void Update()
    {
        if (currentState == PreviewState.Idle) return;

        HoverInfo hover = InputManager.Instance != null
            ? InputManager.Instance.GetHoverTarget()
            : GetHoverTargetFallback();

        DispatchHover(hover);
    }

    // ====================================================================
    //  初始化
    // ====================================================================

    void ResolveReferences()
    {
        if (gridVisualizer == null) gridVisualizer = FindObjectOfType<GridVisualizer>();
        if (unitVisualizer == null) unitVisualizer = FindObjectOfType<UnitVisualizer>();
        if (pathRenderer == null) pathRenderer = GetComponentInChildren<PathRenderer>();

        if (gridVisualizer == null) Logger.LogWarning("PreviewManager: 未找到 GridVisualizer");
        if (unitVisualizer == null) Logger.LogWarning("PreviewManager: 未找到 UnitVisualizer");
    }

    void RegisterBehaviours()
    {
        var gridMove = new GridMoveBehaviour();
        var unitSelect = new UnitSelectBehaviour();

        gridMove.Initialize(gridVisualizer, unitVisualizer, pathRenderer,
            () => InputManager.Instance?.GetHoverTarget() ?? default,
            ShowPreselectAt, HidePreselect,
            ShowBoxOnUnit, HideBox);

        unitSelect.Initialize(gridVisualizer, unitVisualizer, pathRenderer,
            () => InputManager.Instance?.GetHoverTarget() ?? default,
            ShowPreselectAt, HidePreselect,
            ShowBoxOnUnit, HideBox);

        behaviours[PreviewType.GridMove] = gridMove;
        behaviours[PreviewType.UnitSelect] = unitSelect;
    }

    void InitializeVisuals()
    {
        // 遮罩
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform mask = mainCam.transform.Find("PreviewMask");
            if (mask != null) previewMask = mask.gameObject;
            else Logger.LogWarning("PreviewManager: 相机下未找到 PreviewMask");
        }

        // 三角尖
        if (markerPrefab != null)
        {
            marker = Instantiate(markerPrefab, transform);
            markerAnimator = marker.GetComponent<Animator>();
            marker.transform.position = new Vector3(-999f, -999f, -999f);
        }
        else Logger.LogWarning("PreviewManager: markerPrefab 未赋值");

        // 预选框
        if (boxPrefab != null)
        {
            box = Instantiate(boxPrefab, transform);
            boxAnimator = box.GetComponent<Animator>();
            box.transform.position = new Vector3(-999f, -999f, boxHeightOffset);
        }
        else Logger.LogWarning("PreviewManager: boxPrefab 未赋值");
    }

    // ====================================================================
    //  输入订阅
    // ====================================================================

    void SubscribeInput()
    {
        GameEventChannel.Register<CellLeftClickedEvent>(OnCellClicked);
        GameEventChannel.Register<CellRightClickedEvent>(OnRightClicked);
        GameEventChannel.Register<UnitLeftClickedEvent>(OnUnitClicked);
        GameEventChannel.Register<EscapePressedEvent>(OnEscPressed);
    }

    void UnsubscribeInput()
    {
        GameEventChannel.Unregister<CellLeftClickedEvent>(OnCellClicked);
        GameEventChannel.Unregister<CellRightClickedEvent>(OnRightClicked);
        GameEventChannel.Unregister<UnitLeftClickedEvent>(OnUnitClicked);
        GameEventChannel.Unregister<EscapePressedEvent>(OnEscPressed);
    }

    // ====================================================================
    //  行为调度
    // ====================================================================

    PreviewBehaviour CurrentBehaviour => CurrentStep != null
        && behaviours.TryGetValue(CurrentStep.Type, out var b) ? b : null;

    // ====================================================================
    //  公共视觉工具（供 Behaviour 回调使用）
    // ====================================================================

    void ShowPreselectAt(Vector2Int gridPos)
    {
        if (marker == null) return;
        Logger.Log($"[Preview] ShowPreselectAt ({gridPos.x},{gridPos.y})");
        if (markerRoutine != null) StopCoroutine(markerRoutine);
        markerRoutine = StartCoroutine(AnimateMarkerShow(gridPos));
    }

    void HidePreselect()
    {
        if (marker == null) return;
        Logger.Log("[Preview] HidePreselect");
        if (markerRoutine != null) StopCoroutine(markerRoutine);
        markerRoutine = StartCoroutine(AnimateMarkerHide());
    }

    IEnumerator AnimateMarkerShow(Vector2Int gridPos)
    {
        Vector3 w = GridManager.Instance.GridToWorld(gridPos);
        marker.transform.position = new Vector3(w.x, w.y + heightOffset, w.z + markerZOffset);
        marker.SetActive(true);

        if (markerAnimator != null)
        {
            // 给 Hide 动画一帧启动时间，防止快速点击时触发堆积
            markerAnimator.ResetTrigger("Show");
            markerAnimator.SetTrigger("Hide");
            yield return null;
            markerAnimator.ResetTrigger("Hide");
            markerAnimator.SetTrigger("Show");
        }
    }

    IEnumerator AnimateMarkerHide()
    {
        if (markerAnimator != null)
        {
            markerAnimator.ResetTrigger("Show");
            markerAnimator.SetTrigger("Hide");
        }
        marker.transform.position = new Vector3(-999f, -999f, -999f);
        yield break;
    }

    void ShowBoxOnUnit(Unit unit)
    {
        if (box == null || unit == null) return;
        Logger.Log($"[Preview] ShowBoxOnUnit: {unit.UnitId}");
        if (boxRoutine != null) StopCoroutine(boxRoutine);
        boxRoutine = StartCoroutine(AnimateBoxShow(unit));
    }

    void HideBox()
    {
        if (box == null) return;
        Logger.Log("[Preview] HideBox");
        if (boxRoutine != null) StopCoroutine(boxRoutine);
        boxRoutine = StartCoroutine(AnimateBoxHide());
    }

    IEnumerator AnimateBoxShow(Unit unit)
    {
        box.transform.position = unit.transform.position + Vector3.up * boxHeightOffset;
        box.SetActive(true);

        if (boxAnimator != null)
        {
            boxAnimator.ResetTrigger("Show");
            boxAnimator.SetTrigger("Hide");
            yield return null;
            boxAnimator.ResetTrigger("Hide");
            boxAnimator.SetTrigger("Show");
        }
    }

    IEnumerator AnimateBoxHide()
    {
        if (boxAnimator != null)
        {
            boxAnimator.ResetTrigger("Show");
            boxAnimator.SetTrigger("Hide");
        }
        box.transform.position = new Vector3(-999f, -999f, boxHeightOffset);
        yield break;
    }

    void ShowOverlayMask()
    {
        if (previewMask != null) previewMask.SetActive(true);
    }

    void HideOverlayMask()
    {
        if (previewMask != null) previewMask.SetActive(false);
    }

    // ====================================================================
    //  悬停调度
    // ====================================================================

    void DispatchHover(HoverInfo hover)
    {
        // 只向当前行为发送匹配类型的悬停
        var beh = CurrentBehaviour;
        if (beh != null)
        {
            beh.OnHover(hover);
        }
        else
        {
            // 无行为时清除所有悬停
            ClearHoverState();
        }
    }

    void ClearHoverState()
    {
        if (lastHoveredCell.HasValue)
        {
            gridVisualizer?.ClearHoverCell();
            lastHoveredCell = null;
        }
        if (lastHoveredUnit != null)
        {
            unitVisualizer?.ClearHoverUnit();
            lastHoveredUnit = null;
        }
    }

    // ====================================================================
    //  栈管理
    // ====================================================================

    void PushStep(PreviewStep step)
    {
        Logger.Log($"[Preview] PushStep (类型:{step.Type}, 栈深:{previewStack.Count + 1})");
        // 保留被选目标的视觉效果，仅清除未选候选和悬停
        if (CurrentStep != null)
        {
            ClearHoverState();
            CurrentBehaviour?.PauseVisuals();
        }

        previewStack.Push(step);
        currentState = PreviewState.Selecting;
        ShowOverlayMask();
        CurrentBehaviour?.OnEnter(step);
        Logger.Log($"PreviewManager: 推入步骤 (类型:{step.Type}, 栈深:{previewStack.Count})");
    }

    void PopStep()
    {
        if (previewStack.Count == 0) return;
        Logger.Log($"[Preview] PopStep (栈深:{previewStack.Count} → {previewStack.Count - 1})");

        // 先出栈，再清理视觉效果（保持预览到最后一刻）
        var prevBehaviour = CurrentBehaviour;
        var step = previewStack.Pop();
        ClearCurrentVisualsWith(prevBehaviour);

        if (previewStack.Count > 0)
        {
            currentState = PreviewState.Selecting;
            CurrentBehaviour?.RestoreVisuals(CurrentStep);
            Logger.Log($"PreviewManager: 弹出步骤，恢复上层 (栈深:{previewStack.Count})");
        }
        else
        {
            currentState = PreviewState.Idle;
            HideOverlayMask();
            Logger.Log("PreviewManager: 所有预览结束");
        }
    }

    void CancelCurrentStep()
    {
        if (previewStack.Count <= 1)
        {
            Logger.Log("[Preview] CancelCurrentStep 被忽略（第一步）");
            return;
        }

        Logger.Log($"[Preview] CancelCurrentStep (栈深:{previewStack.Count})");
        var prevBehaviour = CurrentBehaviour;
        var step = previewStack.Pop();
        step.OnCancel?.Invoke();
        ClearCurrentVisualsWith(prevBehaviour);

        currentState = PreviewState.Selecting;
        CurrentBehaviour?.RestoreVisuals(CurrentStep);
        Logger.Log($"PreviewManager: 取消当前步骤 (栈深:{previewStack.Count})");
    }

    // ====================================================================
    //  视觉管理
    // ====================================================================

    void ClearCurrentVisuals()
    {
        Logger.Log("[Preview] ClearCurrentVisuals");
        ClearHoverState();
        CurrentBehaviour?.ClearVisuals();
        HidePreselect();
        HideBox();
    }

    void ClearCurrentVisualsWith(PreviewBehaviour behaviour)
    {
        Logger.Log("[Preview] ClearCurrentVisualsWith");
        ClearHoverState();
        behaviour?.ClearVisuals();
        HidePreselect();
        HideBox();
    }

    void PauseCurrentVisuals()
    {
        Logger.Log("[Preview] PauseCurrentVisuals");
        ClearHoverState();
        CurrentBehaviour?.ClearVisuals();
        HidePreselect();
        HideBox();
        // 遮罩保持
    }

    // ====================================================================
    //  预览入口
    // ====================================================================

    /// <summary>
    /// 进入格子移动预览
    /// </summary>
    public void EnterGridPreview(Unit unit, List<Vector2Int> cells,
        Action<Vector2Int> onConfirm, Action onCancel)
    {
        if (cells == null || cells.Count == 0)
        {
            Logger.LogWarning("PreviewManager.EnterGridPreview: 候选格子为空");
            onCancel?.Invoke();
            return;
        }

        PushStep(new PreviewStep
        {
            Type = PreviewType.GridMove,
            CandidateCells = cells,
            CurrentUnit = unit,
            OnCellConfirm = onConfirm,
            OnCancel = onCancel
        });
    }

    /// <summary>
    /// 进入单位选择预览
    /// </summary>
    public void EnterUnitPreview(List<Unit> candidates,
        Action<Unit> onConfirm, Action onCancel)
    {
        if (candidates == null || candidates.Count == 0)
        {
            Logger.LogWarning("PreviewManager.EnterUnitPreview: 候选单位为空");
            onCancel?.Invoke();
            return;
        }

        PushStep(new PreviewStep
        {
            Type = PreviewType.UnitSelect,
            CandidateUnits = candidates,
            OnUnitConfirm = onConfirm,
            OnCancel = onCancel
        });
    }

    /// <summary>
    /// 确认并弹出当前步骤（由调用方在 onConfirm 末尾调用）
    /// </summary>
    public void PopCurrentStep()
    {
        PopStep();
    }

    // ====================================================================
    //  输入处理
    // ====================================================================

    void OnCellClicked(CellLeftClickedEvent evt)
    {
        if (currentState == PreviewState.Idle) return;
        CurrentBehaviour?.OnCellClick(evt.GridPosition);
    }

    void OnUnitClicked(UnitLeftClickedEvent evt)
    {
        if (currentState == PreviewState.Idle) return;
        CurrentBehaviour?.OnUnitClick(evt.Unit);
    }

    void OnRightClicked(CellRightClickedEvent evt)
    {
        if (currentState != PreviewState.Idle)
            CancelCurrentStep();
    }

    void OnEscPressed(EscapePressedEvent evt)
    {
        if (currentState != PreviewState.Idle)
            CancelCurrentStep();
    }

    // ====================================================================
    //  调试方法
    // ====================================================================

    public PreviewStep GetCurrentStep() => CurrentStep;
    public PreviewState GetPreviewState() => currentState;
    public Stack<PreviewStep> GetPreviewStack() => previewStack;
    // 兼容旧名
    public PreviewStep GetPreviewContext() => CurrentStep;

    // ====================================================================
    //  备用悬停（InputManager 不可用时）
    // ====================================================================

    HoverInfo GetHoverTargetFallback()
    {
        Camera cam = Camera.main;
        if (cam == null) return default;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        HoverInfo info = default;

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null && unit.IsAlive)
            {
                info.unit = unit;
                info.cellPosition = unit.GridPosition;
                return info;
            }

            if (GridManager.Instance != null)
            {
                GridManager.Instance.WorldToGrid(hit.point, out int c, out int r);
                if (GridManager.Instance.GetCell(c, r) != null)
                    info.cellPosition = new Vector2Int(c, r);
            }
        }
        return info;
    }
}
