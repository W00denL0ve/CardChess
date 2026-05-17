using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 返回以 context.executed 为中心的半径内所有格子
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CellsAroundExecuted")]
public class CellsAroundExecutedSelector : TargetSelector
{
    /// <summary>半径（格数）</summary>
    public int radius = 1;

    /// <summary>是否只返回可行走的格子</summary>
    public bool onlyWalkable;

    /// <summary>
    /// 返回以 context.executed 为中心的半径内所有格子
    /// </summary>
    public override List<ITarget> GetTargets(EffectContext context)
    {
        var center = context.GetExecutedCell();
        if (center == null) center = context.GetExecutorUnit()?.GridPosition;
        if (center == null) return new List<ITarget>();

        List<ITarget> cells = new List<ITarget>();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int pos = new Vector2Int(center.Value.x + dx, center.Value.y + dy);

                if (onlyWalkable && GridManager.Instance != null)
                {
                    Cell cell = GridManager.Instance.GetCell(pos.x, pos.y);
                    if (cell == null || !cell.isWalkable) continue;
                }

                cells.Add(new CellTarget(pos));
            }
        }
        return cells;
    }

    public override void PreviewHighlight(EffectContext context, bool show)
    {
        var vis = Object.FindObjectOfType<GridVisualizer>();
        if (vis == null) return;

        if (show)
        {
            var targets = GetTargets(context);
            var positions = targets.Select(t => t.GetCellPosition()).Where(p => p.HasValue).Select(p => p.Value).ToList();
            if (positions.Count > 0) vis.HighlightCells(positions);
        }
        else
        {
            vis.ClearHighlights();
        }
    }
}
