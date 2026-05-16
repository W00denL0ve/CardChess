using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // 旧版回调（兼容）
    private Action<Cell> cellSelectionCallback;
    private List<Cell> allowedCells;

    [Header("Input Settings")]
    [SerializeField] private LayerMask groundLayer = 1 << 0;

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
            // Click 同时承载单击和双击（通过 MultiTap Interaction 区分）
            gameInput.Gameplay.Click.performed += OnClickPerformed;
            gameInput.Gameplay.ContextMenu.performed += OnContextMenuPerformed;
            gameInput.Gameplay.Escape.performed += OnEscapePerformed;
        }
    }

    private void OnDisable()
    {
        if (gameInput != null)
        {
            gameInput.Gameplay.Click.performed -= OnClickPerformed;
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
    /// Click 动作的 performed 回调 — 通过 interaction 类型区分单击/双击
    /// </summary>
    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        Vector2Int? gridPos = GetGridUnderMouse();
        if (!gridPos.HasValue) return;

        bool isDoubleClick = false;

        if (context.interaction is MultiTapInteraction multiTap)
        {
            isDoubleClick = multiTap.tapCount >= 2;
        }

        if (isDoubleClick)
        {
            GameEventChannel.Dispatch(new CellDoubleClickedEvent(gridPos.Value));
        }
        else
        {
            GameEventChannel.Dispatch(new CellLeftClickedEvent(gridPos.Value));
        }

        // 旧版回调兼容
        Cell cell = GridManager.Instance?.GetCell(gridPos.Value.x, gridPos.Value.y);
        if (cell != null)
            OnCellClicked(cell);
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
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
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