using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

/// <summary>
/// 悬停目标信息
/// </summary>
public struct HoverInfo
{
    public Vector2Int? cellPosition;
    public Unit unit;
    public bool isValid => cellPosition.HasValue || unit != null;
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // 旧版回调（兼容）
    private Action<Cell> cellSelectionCallback;
    private List<Cell> allowedCells;

    [Header("Input Settings")]
    [SerializeField] private LayerMask cellLayerMask = 1 << 0;
    [SerializeField] private LayerMask unitLayerMask = 1 << 0;

    private float longPressThreshold = 0.3f;                      // 运行时从 Input Action 读取
    private InputAction longPressAction;                          // LongPress 动作引用
    private float pressStartTime = 0f;
    private ILongPressTarget pendingLongPressTarget = null;                     // 按下时检测到的可长按目标
    private bool longPressPerformed = false;                      // 是否已达阈值（防重复派发）

    // New Input System
    private GameInput gameInput;

    private Camera mainCamera;

    // ====================================================================
    //  生命周期
    // ====================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        gameInput = new GameInput();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        Logger.Log($"获取到相机{mainCamera.name}");

        // 从 Input Action 的 Hold 交互一次读取阈值，与配置保持同步
        ReadLongPressThreshold();
    }

    private void OnEnable()
    {
        gameInput?.Enable();

        if (gameInput != null)
        {
            gameInput.Gameplay.Click.performed += OnClickPerformed;
            gameInput.Gameplay.DoubleClick.performed += OnDoubleClickPerformed;
            gameInput.Gameplay.ContextMenu.performed += OnContextMenuPerformed;
            gameInput.Gameplay.Escape.performed += OnEscapePerformed;
            gameInput.Gameplay.Press.started += OnLongPressStarted;
            gameInput.Gameplay.Press.canceled += OnLongPressCanceled;
            gameInput.Gameplay.Press.performed += OnLongPressPerformed;
        }
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.Gameplay.Click.performed -= OnClickPerformed;
            gameInput.Gameplay.DoubleClick.performed -= OnDoubleClickPerformed;
            gameInput.Gameplay.ContextMenu.performed -= OnContextMenuPerformed;
            gameInput.Gameplay.Escape.performed -= OnEscapePerformed;
            gameInput.Gameplay.Press.started -= OnLongPressStarted;
            gameInput.Gameplay.Press.canceled -= OnLongPressCanceled;
            gameInput.Gameplay.Press.performed -= OnLongPressPerformed;
            gameInput.Disable();
        }
    }

    private void Update()
    {
        if (pendingLongPressTarget == null) return;

        float progress = Mathf.Clamp01(
            (Time.time - pressStartTime) / longPressThreshold);

        GameEventChannel.Dispatch(new LongPressUpdateEvent(
            pendingLongPressTarget, progress));
    }

    private void OnDestroy()
    {
        gameInput?.Dispose();
    }

    // ====================================================================
    //  输入事件处理 — 全部通过 GameEventChannel 分发
    // ====================================================================

    /// <summary>
    /// Click 动作的 performed 回调 — 每次左键单击触发
    /// 同时检测单位和格子，分别派发事件
    /// </summary>
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // Logger.Log("[Input] Click performed!"); 
        // 检测单位
        Unit unit = GetUnitUnderMouse();
        if (unit != null)
            GameEventChannel.Dispatch(new UnitLeftClickedEvent(unit));

        // 检测格子（始终派发，由接收方根据状态过滤）
        Vector2Int? gridPos = GetGridUnderMouse();
        if (gridPos.HasValue)
        {
            GameEventChannel.Dispatch(new CellLeftClickedEvent(gridPos.Value));

            // 旧版回调兼容
            Cell cell = GridManager.Instance?.GetCell(gridPos.Value.x, gridPos.Value.y);
            if (cell != null)
                OnCellClicked(cell);
        }
    }

    /// <summary>
    /// DoubleClick 动作的 performed 回调 — 双击时触发（MultiTap tapCount=2）
    /// </summary>
    private void OnDoubleClickPerformed(InputAction.CallbackContext context)
    {
        // 先检测单位
        Unit unit = GetUnitUnderMouse();
        if (unit != null)
        {
            GameEventChannel.Dispatch(new UnitDoubleClickedEvent(unit));
            return;
        }

        // 再检测格子
        Vector2Int? gridPos = GetGridUnderMouse();
        if (!gridPos.HasValue) return;

        GameEventChannel.Dispatch(new CellDoubleClickedEvent(gridPos.Value));
    }

    private void OnContextMenuPerformed(InputAction.CallbackContext context)
    {
        Vector2Int? gridPos = GetGridUnderMouse();
        if (gridPos.HasValue)
            GameEventChannel.Dispatch(new CellRightClickedEvent(gridPos.Value));
    }

    private void OnEscapePerformed(InputAction.CallbackContext context)
    {
        GameEventChannel.Dispatch(new EscapePressedEvent());
    }

    // ====================================================================
    //  工具方法
    // ====================================================================

    /// <summary>
    /// 获取鼠标下的格子坐标
    /// </summary>
    Vector2Int? GetGridUnderMouse()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, cellLayerMask))
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.WorldToGrid(hit.point, out int col, out int row);
                if (GridManager.Instance.GetCell(col, row) != null)
                    return new Vector2Int(col, row);
            }
        }
        return null;
    }

    // 按下瞬间
    private void OnLongPressStarted(InputAction.CallbackContext context)
    {
        pressStartTime = Time.time;
        pendingLongPressTarget = GetLongPressTargetUnderMouse();

        if (pendingLongPressTarget != null)
            GameEventChannel.Dispatch(new LongPressStartedEvent(pendingLongPressTarget));
    }

    // 松开瞬间
    // Hold 交互：未达到阈值时松开 → canceled；已达到阈值后松开 → canceled（需区分）
    private void OnLongPressCanceled(InputAction.CallbackContext context)
    {
        // 仅当长按尚未完成时才派发取消事件
        if (pendingLongPressTarget != null && !longPressPerformed)
        {
            GameEventChannel.Dispatch(new LongPressCancelledEvent(pendingLongPressTarget));
        }

        pendingLongPressTarget = null;
        longPressPerformed = false;
    }

    // 长按完成（Hold 交互达到阈值后自动触发）
    private void OnLongPressPerformed(InputAction.CallbackContext context)
    {
        if (pendingLongPressTarget != null)
        {
            longPressPerformed = true;
            GameEventChannel.Dispatch(new LongPressPerformedEvent(pendingLongPressTarget));
            pendingLongPressTarget = null;
        }
    }

    // ---------- 长按阈值读取 ----------
    private void ReadLongPressThreshold()
    {
        // interactions 定义在 action 级别，非 binding 级别
        string interactions = gameInput.Gameplay.Press.interactions;
        if (string.IsNullOrEmpty(interactions) || !interactions.StartsWith("Hold"))
        {
            Logger.LogWarning($"InputManager: 未找到 Hold 交互参数，使用默认值 {longPressThreshold}s");
            return;
        }

        // 解析 "Hold(duration=5)" 格式
        int idx = interactions.IndexOf("duration");
        if (idx < 0) return;

        string param = interactions.Substring(idx + 8);
        if (param.Length > 0 && (param[0] == ':' || param[0] == '='))
            param = param.Substring(1);
        int end = param.IndexOfAny(new[] { ')', ',' });
        if (end > 0) param = param.Substring(0, end);

        if (float.TryParse(param,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float duration))
        {
            longPressThreshold = duration;
            Logger.Log($"InputManager: 读取长按阈值 = {duration}s");
        }
    }

    // ---------- 鼠标检测 ----------

    /// <summary>
    /// 获取鼠标下的单位（使用 unitLayerMask 层过滤）
    /// </summary>
    Unit GetUnitUnderMouse()
    {
        if (mainCamera == null || unitLayerMask.value == 0) return null;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, unitLayerMask))
        {
            Unit unit = hit.collider.GetComponentInParent<Unit>();
            if (unit != null && unit.IsAlive)
                return unit;
        }
        return null;
    }

    /// <summary>
    /// 获取鼠标下可长按的目标（实现 ILongPressTarget 的对象）
    /// 目前检测顺序：Unit → 可拓展其他类型
    /// </summary>
    ILongPressTarget GetLongPressTargetUnderMouse()
    {
        // 优先检测单位
        Unit unit = GetUnitUnderMouse();
        if (unit != null) return unit;

        // 未来可在此添加其他 ILongPressTarget 的检测
        // 例如：Cell、Card 等

        return null;
    }

    // ====================================================================
    //  悬停检测
    // ====================================================================

    /// <summary>
    /// 获取鼠标下的悬停目标（格子和单位）
    /// 先按 unitLayerMask 检测单位，再按 cellLayerMask 检测格子
    /// </summary>
    public HoverInfo GetHoverTarget()
    {
        if (mainCamera == null) return default;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        HoverInfo info = default;

        // 按层检测单位
        if (unitLayerMask.value != 0)
        {
            if (Physics.Raycast(ray, out RaycastHit unitHit, 100f, unitLayerMask))
            {
                Unit unit = unitHit.collider.GetComponentInParent<Unit>();
                if (unit != null && unit.IsAlive)
                {
                    info.unit = unit;
                    info.cellPosition = unit.GridPosition;
                    return info;
                }
            }
        }

        // 按层检测格子
        if (cellLayerMask.value != 0)
        {
            if (Physics.Raycast(ray, out RaycastHit cellHit, 100f, cellLayerMask))
            {
                if (GridManager.Instance != null)
                {
                    GridManager.Instance.WorldToGrid(cellHit.point, out int col, out int row);
                    if (GridManager.Instance.GetCell(col, row) != null)
                        info.cellPosition = new Vector2Int(col, row);
                }
            }
        }

        return info;
    }

    // ====================================================================
    //  旧版回调（兼容）
    // ====================================================================

    public void OnCellClicked(Cell cell)
    {
        if (cellSelectionCallback != null && (allowedCells == null || allowedCells.Contains(cell)))
        {
            cellSelectionCallback(cell);
            cellSelectionCallback = null;
            allowedCells = null;
        }
    }

    public void WaitForCellSelection(Action<Cell> callback, List<Cell> allowed = null)
    {
        cellSelectionCallback = callback;
        allowedCells = allowed;
    }
}