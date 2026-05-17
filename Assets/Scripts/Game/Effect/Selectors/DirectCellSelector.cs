using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 直接格子选择器 — 第一类选择器，不依赖上下文执行者链
/// 直接从场景中选择格子，不改变执行者
/// 适用于"卡牌直接对地格生效"的场景
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/DirectCell")]
public class DirectCellSelector : TargetSelector
{
    public enum ShapeType { Circle, Square, Cross, Ring }

    [Header("形状与范围")]
    public ShapeType shape = ShapeType.Circle;
    [Tooltip("最小距离（含），0=中心格")]
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

    public DirectCellSelector()
    {
        ChangesContext = false; // 第一类选择器不改变执行者
    }

    public override List<ITarget> GetTargets(EffectContext context)
    {
        // 以卡牌发出者（初始 executor）为中心
        Unit caster = context.GetExecutorUnit();
        Vector2Int center = caster != null ? caster.GridPosition : Vector2Int.zero;

        int min = Mathf.Min(minRadius, maxRadius);
        int max = Mathf.Max(minRadius, maxRadius);
        var cells = new List<ITarget>();
        var grid = GridManager.Instance;
        if (grid == null) return cells;

        for (int dx = -max; dx <= max; dx++)
        {
            for (int dy = -max; dy <= max; dy++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);

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

                Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                Cell cell = grid.GetCell(pos.x, pos.y);
                if (cell == null) continue;

                if (onlyWalkable && !cell.isWalkable) continue;
                if (onlyUnowned && cell.OccupyingUnit != null) continue;

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
