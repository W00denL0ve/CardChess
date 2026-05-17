using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 格子移动预览行为 — 高亮可选格子、点击预选、双击确认、路径绘制
/// </summary>
public class GridMoveBehaviour : PreviewBehaviour
{
    private PreviewStep step;
    private PreviewState currentState;

    public GridMoveBehaviour()
    {
        Type = PreviewType.GridMove;
    }

    public override void OnEnter(PreviewStep step)
    {
        this.step = step;
        currentState = PreviewState.Selecting;

        // 确保候选格子包含起点（允许原地不动）
        if (step.CurrentUnit != null)
        {
            Vector2Int origin = step.CurrentUnit.GridPosition;
            if (!step.CandidateCells.Contains(origin))
                step.CandidateCells.Add(origin);
        }

        if (step.CandidateCells != null && step.CandidateCells.Count > 0)
            GridVisualizer?.HighlightCells(step.CandidateCells, step.CurrentUnit);

        Logger.Log($"GridMoveBehaviour: 进入，候选 {step.CandidateCells?.Count ?? 0} 格");
    }

    public override void ClearVisuals()
    {
        GridVisualizer?.ClearHighlights();
        PathRenderer?.HidePath();
        HidePreselect?.Invoke();
    }

    public override void RestoreVisuals(PreviewStep step)
    {
        if (step.CandidateCells != null && step.CandidateCells.Count > 0)
            GridVisualizer?.HighlightCells(step.CandidateCells, step.CurrentUnit);

        if (step.PreselectedCell.HasValue)
        {
            ShowPreselectAt?.Invoke(step.PreselectedCell.Value);
            UpdatePath(step.PreselectedCell.Value);
            currentState = PreviewState.Preselected;
        }
    }

    // ==================== 输入事件 ====================

    private Vector2Int? lastHoveredPreviewCell;

    /// <summary>
    /// 点击单位时，如果该单位占据的位置在候选格子中，转为格子点击处理（用于选中原点）
    /// </summary>
    public override void OnUnitClick(Unit unit)
    {
        Vector2Int unitPos = unit.GridPosition;
        if (step.CandidateCells != null && step.CandidateCells.Contains(unitPos))
        {
            OnCellClick(unitPos);
        }
    }

    public override void OnCellClick(Vector2Int pos)
    {
        if (!IsValidCell(pos)) return;

        switch (currentState)
        {
            case PreviewState.Selecting:
                step.PreselectedCell = pos;
                currentState = PreviewState.Preselected;
                ShowPreselectAt?.Invoke(pos);
                UpdatePath(pos);
                Logger.Log($"GridMoveBehaviour: 预选格子 ({pos.x},{pos.y})");
                break;

            case PreviewState.Preselected:
                if (pos != step.PreselectedCell)
                {
                    step.PreselectedCell = pos;
                    ShowPreselectAt?.Invoke(pos);
                    UpdatePath(pos);
                    Logger.Log($"GridMoveBehaviour: 更新预选 ({pos.x},{pos.y})");
                }
                else
                {
                    // 再次点击已预选的格子 → 确认选择
                    Logger.Log($"GridMoveBehaviour: 确认选择 ({pos.x},{pos.y})");
                    step.OnCellConfirm?.Invoke(pos);
                }
                break;
        }
    }

    public override void OnHover(HoverInfo hover)
    {
        Vector2Int? current = hover.cellPosition;
        if (current == lastHoveredPreviewCell) return; // 无变化，跳过
        lastHoveredPreviewCell = current;

        if (current.HasValue && step.CandidateCells.Contains(current.Value))
        {
            GridVisualizer?.SetHoverCell(current.Value);
        }
        else
        {
            GridVisualizer?.ClearHoverCell();
        }
    }

    // ==================== 内部方法 ====================

    bool IsValidCell(Vector2Int pos)
    {
        if (step.CandidateCells == null || !step.CandidateCells.Contains(pos))
        {
            Logger.LogWarning($"GridMoveBehaviour: 位置 ({pos.x},{pos.y}) 不在候选中");
            return false;
        }
        return true;
    }

    void UpdatePath(Vector2Int target)
    {
        if (step.CurrentUnit == null || GridManager.Instance == null) return;
        var path = GridManager.Instance.FindPath(step.CurrentUnit.GridPosition, target);
        PathRenderer?.ShowPath(path);
    }
}
