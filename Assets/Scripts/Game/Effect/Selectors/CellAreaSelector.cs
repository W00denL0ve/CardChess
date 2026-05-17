using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 区域格子选择器（第二类）— 以 context.executor 或 context.executed 为中心，
/// 按形状和距离范围选择格子，可选按格子上单位过滤
/// 会触发上下文链更新：executor ← old executed, executed ← 目标格
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CellAreaSelector")]
public class CellAreaSelector : TargetSelector
{
    public enum CenterSource { Executor, Executed }
    public enum ShapeType { Circle, Square, Cross, Ring }

    [Header("中心")]
    public CenterSource centerSource = CenterSource.Executed;

    [Header("形状与范围")]
    public ShapeType shape = ShapeType.Circle;
    [Tooltip("最小距离（含），0=中心格本身")]
    public int minRadius = 0;
    [Tooltip("最大距离（含），0=仅中心格")]
    public int maxRadius = 1;

    [Header("过滤")]
    [Tooltip("仅包含可行走的格子")]
    public bool onlyWalkable = true;
    [Tooltip("仅包含未被占据的格子")]
    public bool onlyUnowned = false;

    [Header("单位过滤（可选）")]
    [Tooltip("仅选择格子上有单位的格子，并满足以下条件")]
    public bool requireUnitOnCell = false;
    public Faction unitFaction = Faction.Enemy;
    public Occupation unitOccupation;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        // 确定中心
        Vector2Int? center = null;
        if (centerSource == CenterSource.Executed)
            center = context.GetExecutedCell();
        else
        {
            Unit execUnit = context.GetExecutorUnit();
            if (execUnit != null) center = execUnit.GridPosition;
        }

        if (center == null) return new List<ITarget>();

        int min = Mathf.Min(minRadius, maxRadius);
        int max = Mathf.Max(minRadius, maxRadius);
        var cells = new List<ITarget>();
        var grid = GridManager.Instance;
        if (grid == null) return cells;

        for (int dx = -max; dx <= max; dx++)
        {
            for (int dy = -max; dy <= max; dy++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy); // 曼哈顿距离

                // 形状过滤
                bool passShape = shape switch
                {
                    ShapeType.Circle => dist <= max && dist >= min,
                    ShapeType.Square => Mathf.Abs(dx) <= max && Mathf.Abs(dy) <= max
                                        && Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) >= min,
                    ShapeType.Cross => (dx == 0 || dy == 0) && dist <= max && dist >= min,
                    ShapeType.Ring => dist >= min && dist <= max,
                    _ => false
                };

                if (!passShape) continue;

                Vector2Int pos = new Vector2Int(center.Value.x + dx, center.Value.y + dy);
                Cell cell = grid.GetCell(pos.x, pos.y);
                if (cell == null) continue;

                // 可行走
                if (onlyWalkable && !cell.isWalkable) continue;
                // 未被占据
                if (onlyUnowned && cell.OccupyingUnit != null) continue;

                // 单位过滤
                if (requireUnitOnCell)
                {
                    Unit u = cell.OccupyingUnit;
                    if (u == null || !u.IsAlive) continue;
                    if (u.Faction != unitFaction) continue;
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
            var positions = targets
                .Select(t => t.GetCellPosition())
                .Where(p => p.HasValue).Select(p => p.Value).ToList();
            if (positions.Count > 0) vis.HighlightCells(positions);
        }
        else
        {
            vis.ClearHighlights();
        }
    }
}
