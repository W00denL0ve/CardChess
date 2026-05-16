using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 网格预览管理器 - 状态机驱动，管理格子高亮、路径绘制、三角尖标记和输入处理
/// </summary>
public class PreviewManager : MonoBehaviour
{
    public static PreviewManager Instance { get; private set; }

    public enum PreviewState { Idle, Selecting, Preselected }

    [Header("Marker")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private float heightOffset = 0.5f;

    // 状态
    private PreviewState currentState = PreviewState.Idle;
    private List<Vector2Int> candidates = new List<Vector2Int>();
    private Vector2Int preselectPos;
    private Action<Vector2Int> onConfirm;
    private Action onCancel;
    private Unit currentUnit;

    // 三角尖
    private GameObject marker;
    private Animator markerAnimator;

    // 遮罩
    private GameObject previewMask;

    // 管理器引用
    private GridVisualizer gridVisualizer;
    private PathRenderer pathRenderer;

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
        // 获取管理器引用
        gridVisualizer = FindObjectOfType<GridVisualizer>();
        if (gridVisualizer == null)
            Logger.LogWarning("PreviewManager: 场景中未找到 GridVisualizer");

        pathRenderer = GetComponentInChildren<PathRenderer>();
        if (pathRenderer == null)
            Logger.LogWarning("PreviewManager: 未找到 PathRenderer 子组件");

        // 查找遮罩对象（相机子物体）
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform maskTransform = mainCam.transform.Find("PreviewMask");
            if (maskTransform != null)
                previewMask = maskTransform.gameObject;
            else
                Logger.LogWarning("PreviewManager: 相机下未找到 PreviewMask 子物体");
        }

        // 初始化三角尖
        InitializeMarker();

        // 通过 GameEventChannel 订阅输入事件
        GameEventChannel.Register<CellLeftClickedEvent>(HandleCellClicked);
        GameEventChannel.Register<CellDoubleClickedEvent>(HandleCellDoubleClicked);
        GameEventChannel.Register<CellRightClickedEvent>(HandleRightClicked);
        GameEventChannel.Register<EscapePressedEvent>(HandleEscPressed);
        Logger.Log("PreviewManager: 已通过 GameEventChannel 订阅输入事件");
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<CellLeftClickedEvent>(HandleCellClicked);
        GameEventChannel.Unregister<CellDoubleClickedEvent>(HandleCellDoubleClicked);
        GameEventChannel.Unregister<CellRightClickedEvent>(HandleRightClicked);
        GameEventChannel.Unregister<EscapePressedEvent>(HandleEscPressed);
    }

    // ====================================================================
    //  三角尖管理
    // ====================================================================

    void InitializeMarker()
    {
        if (markerPrefab == null)
        {
            Logger.LogWarning("PreviewManager: markerPrefab 未赋值");
            return;
        }
        marker = Instantiate(markerPrefab, transform);
        markerAnimator = marker.GetComponent<Animator>();
        // 初始隐藏到远处
        marker.transform.position = new Vector3(-999f, -999f, heightOffset);
        Logger.Log("PreviewManager: 三角尖已初始化");
    }

    void ShowPreselectAt(Vector2Int gridPos)
    {
        if (marker == null) return;
        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        marker.transform.position = new Vector3(worldPos.x, worldPos.y + heightOffset, worldPos.z);
        if (markerAnimator != null)
            markerAnimator.SetTrigger("Show");
        marker.SetActive(true);
    }

    void HidePreselect()
    {
        if (marker == null) return;
        if (markerAnimator != null)
            markerAnimator.SetTrigger("Hide");
        else
            marker.SetActive(false);
    }

    // ====================================================================
    //  遮罩管理
    // ====================================================================

    void ShowOverlayMask()
    {
        if (previewMask != null)
            previewMask.SetActive(true);
    }

    void HideOverlayMask()
    {
        if (previewMask != null)
            previewMask.SetActive(false);
    }

    // ====================================================================
    //  预览生命周期
    // ====================================================================

    /// <summary>
    /// 进入网格预览模式
    /// </summary>
    public void EnterGridPreview(Unit unit, List<Vector2Int> cells,
        Action<Vector2Int> confirm, Action cancel)
    {
        if (unit == null || cells == null || cells.Count == 0)
        {
            Logger.LogWarning("PreviewManager.EnterGridPreview: 参数无效");
            return;
        }

        currentUnit = unit;
        candidates = cells;
        onConfirm = confirm;
        onCancel = cancel;
        currentState = PreviewState.Selecting;

        ShowOverlayMask();
        gridVisualizer?.HighlightCells(cells);

        Logger.Log($"PreviewManager: 进入网格预览，可选格子 {cells.Count} 个");
    }

    /// <summary>
    /// 退出预览模式，清除所有视觉元素
    /// </summary>
    void ExitPreview()
    {
        HideOverlayMask();
        gridVisualizer?.ClearHighlights();
        pathRenderer?.HidePath();
        HidePreselect();
        currentState = PreviewState.Idle;
        currentUnit = null;
        candidates.Clear();
        onConfirm = null;
        onCancel = null;
        Logger.Log("PreviewManager: 退出预览");
    }

    /// <summary>
    /// 确认选择并触发回调
    /// </summary>
    void ConfirmSelection(Vector2Int pos)
    {
        var confirm = onConfirm;
        Logger.Log($"PreviewManager: 确认选择 ({pos.x}, {pos.y})");
        ExitPreview();
        confirm?.Invoke(pos);
    }

    /// <summary>
    /// 取消预览并触发取消回调
    /// </summary>
    void CancelPreview()
    {
        if (currentState == PreviewState.Idle) return;
        var cancel = onCancel;
        Logger.Log("PreviewManager: 取消预览");
        ExitPreview();
        cancel?.Invoke();
    }

    // ====================================================================
    //  路径更新
    // ====================================================================

    void UpdatePath(Vector2Int target)
    {
        if (currentUnit == null || GridManager.Instance == null) return;
        var path = GridManager.Instance.FindPath(currentUnit.gridPosition, target);
        pathRenderer?.ShowPath(path);
    }

    // ====================================================================
    //  输入处理
    // ====================================================================

    void HandleCellClicked(CellLeftClickedEvent evt)
    {
        Vector2Int pos = evt.GridPosition;
        if (currentState == PreviewState.Idle) return;

        // 检查点击位置是否在候选格子中
        if (!candidates.Contains(pos))
        {
            Logger.LogWarning($"PreviewManager: 点击位置 ({pos.x},{pos.y}) 不在候选格子中");
            return;
        }

        switch (currentState)
        {
            case PreviewState.Selecting:
                // 单击 → 进入预选状态
                preselectPos = pos;
                currentState = PreviewState.Preselected;
                ShowPreselectAt(pos);
                UpdatePath(pos);
                Logger.Log($"PreviewManager: 预选格子 ({pos.x}, {pos.y})");
                break;

            case PreviewState.Preselected:
                if (pos != preselectPos)
                {
                    // 单击不同格子 → 更新预选
                    HidePreselect();
                    preselectPos = pos;
                    ShowPreselectAt(pos);
                    UpdatePath(pos);
                    Logger.Log($"PreviewManager: 更新预选格子 ({pos.x}, {pos.y})");
                }
                // 单击同一格子 → 无操作，等待双击或右键
                break;
        }
    }

    /// <summary>
    /// 双击格子 → 直接确认选择（跳过预选）
    /// </summary>
    void HandleCellDoubleClicked(CellDoubleClickedEvent evt)
    {
        Vector2Int pos = evt.GridPosition;
        if (currentState == PreviewState.Idle) return;

        if (!candidates.Contains(pos))
        {
            Logger.LogWarning($"PreviewManager: 双击位置 ({pos.x},{pos.y}) 不在候选格子中");
            return;
        }

        // 双击某个候选格子 → 立即确认
        // 如果处于 Selecting 状态，先显示标记再确认
        if (currentState == PreviewState.Selecting)
        {
            preselectPos = pos;
            ShowPreselectAt(pos);
            UpdatePath(pos);
        }
        ConfirmSelection(pos);
    }

    void HandleRightClicked(CellRightClickedEvent evt)
    {
        if (currentState != PreviewState.Idle)
            CancelPreview();
    }

    void HandleEscPressed(EscapePressedEvent evt)
    {
        if (currentState != PreviewState.Idle)
            CancelPreview();
    }
}