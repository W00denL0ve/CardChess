using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 移动范围格子选择器（第二类）— 以 context.executor 或 context.executed 为中心，
/// 范围自动取该单位（被执行者）的 ActionPointLimit
/// 适用于"选一个地方走过去"的效果链
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/MoveRangeCell")]
public class MoveRangeCellSelector : TargetSelector
{
    public enum CenterSource { Executor, Executed }

    [Header("中心")]
    public CenterSource centerSource = CenterSource.Executed;

    [Header("范围设置")]
    [Tooltip("为 true 时最终步数取 range 与单位 ActionPointLimit 的较小值")]
    public bool clampToActionPointLimit = true;

    [Tooltip("固定步数偏移量（与行动力相加或取 clamp）")]
    public int rangeOffset = 0;

    [Header("过滤")]
    public bool includeOrigin = true;
    public bool ignoreOccupied = false;
    public bool canPassUnwalkable = false;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        // 确定中心单位
        Unit centerUnit = centerSource == CenterSource.Executed
            ? context.GetExecutedUnit()
            : context.GetExecutorUnit();

        if (centerUnit == null) return new List<ITarget>();

        Vector2Int center = centerUnit.GridPosition;
        int actionPoints = centerUnit.ActionPointLimit;
        int range = actionPoints + rangeOffset;
        if (range < 0) range = 0;
        if (clampToActionPointLimit)
            range = Mathf.Min(range, centerUnit.ActionPointLimit);

        var candidates = GridManager.Instance?.GetReachableCells(
            center, range, ignoreOccupied, canPassUnwalkable
        );

        if (candidates == null) return new List<ITarget>();

        if (!includeOrigin)
            candidates.Remove(center);

        return candidates.ConvertAll(c => (ITarget)new CellTarget(c));
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
