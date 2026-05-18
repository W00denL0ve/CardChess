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

    [Header("Cell Pin")]
    [SerializeField] private GameObject cellPinPrefab;
    [SerializeField] private float cellPinYOffset = 0.5f;
    [SerializeField] private float cellPinZOffset = 0f;

    [Header("Unit Pin")]
    [SerializeField] private GameObject unitPinPrefab;
    [SerializeField] private float unitPinYOffset = 0.5f;
    [SerializeField] private float unitPinZOffset = 0.3f;

    [Header("池大小")]
    [SerializeField] private int poolSize = 3;

    [Header("遮罩")]
    private GameObject overlayMask;

    // ── Pin 池（分类型）──
    private Queue<GameObject> cellPinPool = new();
    private Queue<GameObject> unitPinPool = new();
    private List<GameObject> fixedPins = new();

    // ── 状态 ──
    private bool isSelecting;
    private bool isCellType;
    private List<Vector2Int> cellCandidates;
    private List<Unit> unitCandidates;
    private Unit currentUnit;
    private Action<Vector2Int> onCellConfirm;
    private Action<Unit> onUnitConfirm;

    // ── 预选 ──
    private bool hasPreselect;
    private Vector2Int preselectedCell;
    private Unit preselectedUnit;
    // ── 回退 ──
    private bool canRevert;
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
        var cam = Camera.main;
        if (cam != null)
        {
            var t = cam.transform.Find("PreviewMask");
            if (t != null) overlayMask = t.gameObject;
        }
        if (overlayMask != null) overlayMask.SetActive(false);

        for (int i = 0; i < poolSize; i++)
        {
            var cp = Instantiate(cellPinPrefab, transform);
            cp.SetActive(false);
            cellPinPool.Enqueue(cp);

            var up = Instantiate(unitPinPrefab, transform);
            up.SetActive(false);
            unitPinPool.Enqueue(up);
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

    public void StartCellPreview(Unit unit, List<Vector2Int> cells, Action<Vector2Int> onConfirm, bool canRevert = false)
    {
        if (cells == null || cells.Count == 0) return;
        this.canRevert = canRevert;
        GridVisualizer.Instance?.ClearHighlights();
        GridVisualizer.Instance?.HighlightCells(cells, unit);
        hasPreselect = false;
        isSelecting = true;
        isCellType = true;
        cellCandidates = cells;
        currentUnit = unit;
        onCellConfirm = onConfirm;
        onUnitConfirm = null;
        if (overlayMask != null) overlayMask.SetActive(true);
        Logger.Log($"[Preview] StartCellPreview: {cells.Count} items");
    }

    public void StartUnitPreview(List<Unit> candidates, Action<Unit> onConfirm, bool canRevert = false)
    {
        if (candidates == null || candidates.Count == 0) return;
        this.canRevert = canRevert;
        UnitVisualizer.Instance?.ClearHighlights();
        UnitVisualizer.Instance?.HighlightUnits(candidates);
        unitCandidates = new List<Unit>(candidates);
        hasPreselect = false;
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
        ClearAllPins();
        if (overlayMask != null) overlayMask.SetActive(false);
        isSelecting = false;
        hasPreselect = false;
        Logger.Log("[Preview] ClearAll");
    }

    /// <summary>自动确认当前预览（仅1个候选时调用）</summary>
    public void AutoConfirmSelection()
    {
        if (!isSelecting) return;

        if (isCellType && cellCandidates != null && cellCandidates.Count == 1)
        {
            var pos = cellCandidates[0];
            var w = GridManager.Instance.GridToWorld(pos);
            AddPin(w, true);
            hasPreselect = false;
            isSelecting = false;
            onCellConfirm?.Invoke(pos);
        }
        else if (!isCellType && unitCandidates != null && unitCandidates.Count == 1)
        {
            var unit = unitCandidates[0];
            AddPin(unit.transform.position, false);
            hasPreselect = false;
            isSelecting = false;
            onUnitConfirm?.Invoke(unit);
        }
    }

    // ====================================================================
    //  Pin 管理
    // ====================================================================

    void AddPin(Vector3 worldPos, bool isCell)
    {
        var pool = isCell ? cellPinPool : unitPinPool;
        var prefab = isCell ? cellPinPrefab : unitPinPrefab;
        float yOff = isCell ? cellPinYOffset : unitPinYOffset;
        float zOff = isCell ? cellPinZOffset : unitPinZOffset;
        var pin = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
        pin.transform.position = new Vector3(worldPos.x, worldPos.y + yOff, worldPos.z + zOff);
        pin.SetActive(true);
        var anim = pin.GetComponent<Animator>();
        if (anim != null) { anim.ResetTrigger("Hide"); anim.SetTrigger("Show"); }
        fixedPins.Add(pin);
    }

    void ClearAllPins()
    {
        var pool = cellPinPool;
        foreach (var p in fixedPins)
        {
            p.SetActive(false);
            // 判断 pin 属于哪个池
            if (p.name.Contains("Cell") || p.name == cellPinPrefab.name + "(Clone)")
                cellPinPool.Enqueue(p);
            else
                unitPinPool.Enqueue(p);
        }
        fixedPins.Clear();
    }

    void RecycleLastPin()
    {
        if (fixedPins.Count == 0) return;
        var pin = fixedPins[fixedPins.Count - 1];
        pin.SetActive(false);
        if (pin.name.Contains("Cell") || pin.name == cellPinPrefab.name + "(Clone)")
            cellPinPool.Enqueue(pin);
        else
            unitPinPool.Enqueue(pin);
        fixedPins.RemoveAt(fixedPins.Count - 1);
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
        hasPreselect = false;

        var last = fixedPins[fixedPins.Count - 1];
        last.SetActive(false);
        // 回收到对应池（简单通过名称判断）
        if (last.name.Contains("Cell")) cellPinPool.Enqueue(last);
        else unitPinPool.Enqueue(last);
        fixedPins.RemoveAt(fixedPins.Count - 1);
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

        // 检查是否在候选范围内
        if (cellCandidates == null || !cellCandidates.Contains(pos)) return;

        if (!hasPreselect)
        {
            // 首次点击 → 预选：加 pin，显示路径
            var w = GridManager.Instance.GridToWorld(pos);
            AddPin(w, true);
            if (currentUnit != null)
            {
                var path = GridManager.Instance?.FindPath(currentUnit.GridPosition, pos);
                if (path != null && path.Count > 0) PathRenderer.Instance?.ShowPath(path);
            }
            preselectedCell = pos;
            hasPreselect = true;
            Logger.Log($"[Preview] 预选格子 ({pos.x},{pos.y})");
        }
        else if (pos != preselectedCell)
        {
            // 点击不同格子 → 移除旧 pin，加新 pin，更新路径
            RecycleLastPin();
            preselectedCell = pos;
            var w = GridManager.Instance.GridToWorld(pos);
            AddPin(w, true);
            if (currentUnit != null)
            {
                PathRenderer.Instance?.HidePath();
                var path = GridManager.Instance?.FindPath(currentUnit.GridPosition, pos);
                if (path != null && path.Count > 0) PathRenderer.Instance?.ShowPath(path);
            }
            Logger.Log($"[Preview] 更新预选格子 ({pos.x},{pos.y})");
        }
        else
        {
            // 再次点击同一格子 → 确认
            PathRenderer.Instance?.HidePath();
            hasPreselect = false;
            isSelecting = false;
            onCellConfirm?.Invoke(pos);
            Logger.Log($"[Preview] 确认格子 ({pos.x},{pos.y})");
        }
    }

    void OnUnitClicked(UnitLeftClickedEvent evt)
    {
        if (!isSelecting || isCellType) return;
        var unit = evt.Unit;

        // 检查是否在候选范围内
        if (unitCandidates == null || !unitCandidates.Contains(unit)) return;

        if (!hasPreselect)
        {
            AddPin(unit.transform.position, false);
            preselectedUnit = unit;
            hasPreselect = true;
            Logger.Log($"[Preview] 预选单位 {unit.UnitId}");
        }
        else if (unit != preselectedUnit)
        {
            RecycleLastPin();
            AddPin(unit.transform.position, false);
            preselectedUnit = unit;
            Logger.Log($"[Preview] 更新预选单位 {unit.UnitId}");
        }
        else
        {
            hasPreselect = false;
            isSelecting = false;
            onUnitConfirm?.Invoke(unit);
            Logger.Log($"[Preview] 确认单位 {unit.UnitId}");
        }
    }

    void OnRightClicked(CellRightClickedEvent evt)
    {
        if (!isSelecting || !canRevert) return;
        RevertStep();
    }

    void OnEscPressed(EscapePressedEvent evt)
    {
        if (!isSelecting || !canRevert) return;
        RevertStep();
    }
}
