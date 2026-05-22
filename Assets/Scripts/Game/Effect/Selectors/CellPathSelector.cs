using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 路径格子选择器 — BFS 寻路可达区域
/// 范围默认使用执行者的 ActionPoints，也可固定值
/// 包含路径缓存（cachedPath）供 MoveEffect 使用
/// </summary>
[CreateAssetMenu(menuName = "CardChess/EffectChain/Selectors/CellPath")]
public class CellPathSelector : TargetSelector
{
    [Header("范围")]
    [Tooltip("-1 = 使用执行者的 MovePoints；≥0 = 固定步数")]
    public int range = -1;
    [Tooltip("是否包含起点格")]
    public bool includeOrigin = true;
    [Tooltip("是否忽略路径上的占据单位")]
    public bool ignoreOccupied = false;
    [Tooltip("是否允许穿过不可行走格子（终点仍需可行走）")]
    public bool canPassUnwalkable = false;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        Unit exec = context.GetExecutedUnit();
        if (exec == null) return new List<ITarget>();

        int steps = range >= 0 ? range : exec.baseValue.movePoints;
        var cells = GridManager.Instance?.GetReachableCells(
            exec.GridPosition, steps, ignoreOccupied, canPassUnwalkable);

        if (cells == null) return new List<ITarget>();
        if (includeOrigin && !cells.Contains(exec.GridPosition))
            cells.Add(exec.GridPosition);

        return cells.ConvertAll(c => (ITarget)new CellTarget(c));
    }

    public override void PreviewHighlight(EffectContext context, bool show)
    {
        var vis = GridVisualizer.Instance;
        if (vis == null) return;
        if (show)
        {
            var targets = GetTargets(context);
            var positions = targets.Select(t => t.GetCellPosition()).Where(p => p.HasValue).Select(p => p.Value).ToList();
            if (positions.Count > 0) vis.HighlightCells(positions);
        }
        else { vis.ClearHighlights(); }
    }
}