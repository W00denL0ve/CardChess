using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 预览管理器 — PinBoard 设计
///
/// Selector → StartXxxPreview → 玩家确认 → MarkCompleted（保留 pin + 选中材质）
/// 右键 → 有 pin 则退回上一步，无 pin 则忽略
/// Effect → ClearAll() 清除所有视觉
/// </summary>
public class PreviewManager : MonoBehaviour
{
    public static PreviewManager Instance { get; private set; }

    [Header("Pin 池")]
    [SerializeField] private GameObject pinPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private Sprite cellPinSprite;
    [SerializeField] private Sprite unitPinSprite;

    [Header("偏移（调试用）")]
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private float zOffset = 0.3f;

    [Header("遮罩")]
    [SerializeField] private GameObject overlayMask;

    // ── Pin 池 ──
    private Queue<GameObject> pinPool = new();
    private List<GameObject> fixedPins = new();
    private GameObject activePin;
    private Animator activePinAnim;

    // ── 状态 ──
    private bool isSelecting;
    private bool isCellType;
    private List<Vector2Int> cellCandidates;
    private Unit currentUnit;
    private Action<Vector2Int> onCellConfirm;
    private Action<Unit> onUnitConfirm;

    // ── 悬停 ──
    private Vector2Int? lastHoveredCell;
    private Unit lastHoveredUnit;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (overlayMask == null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var t = cam.transform.Find("PreviewMask");
                if (t != null) overlayMask = t.gameObject;
            }
        }
        for (int i = 0; i < poolSize; i++)
        {
            var p = Instantiate(pinPrefab, transform);
            p.SetActive(false);
            pinPool.Enqueue(p);
        }
        SubscribeInput();
    }

    void OnDestroy() => UnsubscribeInput();

    void Update()
    {
        if (!isSelecting) return;
        var hover = InputManager.Instance?.GetHoverTarget() ?? default;
        UpdateHover(hover);
    }

    // ====================================================================
    //  公开 API
    // ====================================================================

    public void StartCellPreview(Unit unit, List<Vector2Int> cells, Action<Vector2Int> onConfirm)
    {
        if (cells == null || cells.Count == 0) return;
        GridVisualizer.Instance?.ClearHighlights();
        PushPin();
        GridVisualizer.Instance?.HighlightCells(cells, unit);
        isSelecting = true;
        isCellType = true;
        cellCandidates = cells;
        currentUnit = unit;
        onCellConfirm = onConfirm;
        onUnitConfirm = null;
        if (overlayMask != null) overlayMask.SetActive(true);
        Logger.Log($"[Preview] StartCellPreview: {cells.Count} items");
    }

    public void StartUnitPreview(List<Unit> candidates, Action<Unit> onConfirm)
    {
        if (candidates == null || candidates.Count == 0) return;
        UnitVisualizer.Instance?.ClearHighlights();
        PushPin();
        UnitVisualizer.Instance?.HighlightUnits(candidates);
        isSelecting = true;
        isCellType = false;
        onUnitConfirm = onConfirm;
        onCellConfirm = null;
        if (overlayMask != null) overlayMask.SetActive(true);
        Logger.Log($"[Preview] StartUnitPreview: {candidates.Count} items");
    }

    /// <summary>清除所有预览视觉（效果执行前调用）</summary>
    public void ClearAll()
    {
        ClearHover();
        GridVisualizer.Instance?.ClearAll();
        UnitVisualizer.Instance?.ClearAll();
        PathRenderer.Instance?.HidePath();
        if (activePin != null) { activePin.SetActive(false); pinPool.Enqueue(activePin); activePin = null; }
        foreach (var p in fixedPins) { p.SetActive(false); pinPool.Enqueue(p); }
        fixedPins.Clear();
        if (overlayMask != null) overlayMask.SetActive(false);
        isSelecting = false;
        Logger.Log("[Preview] ClearAll");
    }

    // ====================================================================
    //  Pin 管理
    // ====================================================================

    void PushPin()
    {
        if (activePin != null) { fixedPins.Add(activePin); }
        activePin = pinPool.Count > 0 ? pinPool.Dequeue() : Instantiate(pinPrefab, transform);
        activePinAnim = activePin.GetComponent<Animator>();
    }

    void ShowActivePin(Vector3 worldPos, Sprite sprite)
    {
        if (activePin == null) return;
        activePin.transform.position = new Vector3(worldPos.x, worldPos.y + heightOffset, worldPos.z + zOffset);
        var sr = activePin.GetComponent<SpriteRenderer>();
        if (sr != null && sprite != null) sr.sprite = sprite;
        activePin.SetActive(true);
        if (activePinAnim != null) { activePinAnim.ResetTrigger("Hide"); activePinAnim.SetTrigger("Show"); }
    }

    // ====================================================================
    //  退回一步
    // ====================================================================

    void RevertStep()
    {
        if (fixedPins.Count == 0) { isSelecting = false; return; }

        ClearHover();
        GridVisualizer.Instance?.ClearHighlights();
        UnitVisualizer.Instance?.ClearHighlights();
        if (activePin != null) { activePin.SetActive(false); pinPool.Enqueue(activePin); }

        var last = fixedPins[fixedPins.Count - 1];
        last.SetActive(false); pinPool.Enqueue(last);
        fixedPins.RemoveAt(fixedPins.Count - 1);

        activePin = fixedPins.Count > 0 ? fixedPins[fixedPins.Count - 1] : null;
        activePinAnim = activePin?.GetComponent<Animator>();
        if (activePin != null) fixedPins.RemoveAt(fixedPins.Count - 1);
    }

    // ====================================================================
    //  悬停
    // ====================================================================

    void UpdateHover(HoverInfo hover)
    {
        if (isCellType)
        {
            if (hover.cellPosition.HasValue && hover.cellPosition != lastHoveredCell)
            {
                if (lastHoveredCell.HasValue) GridVisualizer.Instance?.ClearHoverCell();
                GridVisualizer.Instance?.SetHoverCell(hover.cellPosition.Value);
                lastHoveredCell = hover.cellPosition;
            }
            else if (!hover.cellPosition.HasValue && lastHoveredCell.HasValue)
            {
                GridVisualizer.Instance?.ClearHoverCell();
                lastHoveredCell = null;
            }
        }
        else
        {
            if (hover.unit != null && hover.unit != lastHoveredUnit)
            {
                UnitVisualizer.Instance?.ClearHoverUnit();
                UnitVisualizer.Instance?.SetHoverUnit(hover.unit);
                lastHoveredUnit = hover.unit;
            }
            else if (hover.unit == null && lastHoveredUnit != null)
            {
                UnitVisualizer.Instance?.ClearHoverUnit();
                lastHoveredUnit = null;
            }
        }
    }

    void ClearHover()
    {
        if (lastHoveredCell.HasValue) { GridVisualizer.Instance?.ClearHoverCell(); lastHoveredCell = null; }
        if (lastHoveredUnit != null) { UnitVisualizer.Instance?.ClearHoverUnit(); lastHoveredUnit = null; }
    }

    // ====================================================================
    //  输入
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

    void OnCellClicked(CellLeftClickedEvent evt)
    {
        if (!isSelecting || !isCellType) return;
        var pos = evt.GridPosition;

        var w = GridManager.Instance.GridToWorld(pos);
        ShowActivePin(w, cellPinSprite);

        if (currentUnit != null)
        {
            var path = GridManager.Instance?.FindPath(currentUnit.GridPosition, pos);
            if (path != null && path.Count > 0) PathRenderer.Instance?.ShowPath(path);
        }

        GridVisualizer.Instance?.ClearHighlights();
        isSelecting = false;
        onCellConfirm?.Invoke(pos);
    }

    void OnUnitClicked(UnitLeftClickedEvent evt)
    {
        if (!isSelecting || isCellType) return;
        var unit = evt.Unit;
        ShowActivePin(unit.transform.position, unitPinSprite);
        UnitVisualizer.Instance?.ClearHighlights();
        isSelecting = false;
        onUnitConfirm?.Invoke(unit);
    }

    void OnRightClicked(CellRightClickedEvent evt)
    {
        if (!isSelecting) return;
        if (fixedPins.Count >= 1) RevertStep();
    }

    void OnEscPressed(EscapePressedEvent evt)
    {
        if (!isSelecting) return;
        if (fixedPins.Count >= 1) RevertStep();
    }
}
