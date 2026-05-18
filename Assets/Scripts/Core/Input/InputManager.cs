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
        mainCamera = Camera.main;
        gameInput = new GameInput();
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
            gameInput.Disable();
        }
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