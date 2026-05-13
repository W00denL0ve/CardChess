using System;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private System.Action<Cell> cellSelectionCallback;
    private List<Cell> allowedCells;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnCellClicked(Cell cell)
    {
        if (cellSelectionCallback != null && (allowedCells == null || allowedCells.Contains(cell)))
        {
            cellSelectionCallback(cell);
            cellSelectionCallback = null;
            allowedCells = null;
        }
    }

    public void WaitForCellSelection(System.Action<Cell> callback, List<Cell> allowed = null)
    {
        cellSelectionCallback = callback;
        allowedCells = allowed;
    }
}