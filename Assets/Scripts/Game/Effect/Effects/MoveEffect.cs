using System.Collections;
using UnityEngine;

/// <summary>
/// 移动效果 - 将 context.executor 移动到 context.executed 所在格子
/// 优先使用 context.cachedPath，其次重新计算
/// </summary>
[CreateAssetMenu(fileName = "MoveEffect", menuName = "CardChess/EffectChain/Effects/Move")]
public class MoveEffect : Effect, IAnimatedEffect
{
    [Tooltip("是否需要路径可达")]
    public bool requirePath = true;

    [Tooltip("是否清空行动力")]
    public bool clearPoints = true;

    public override void OnExecute(EffectContext context)
    {
        Unit unit = context.GetExecutorUnit();
        Vector2Int? targetCell = context.GetExecutedCell();
        if (unit == null || !targetCell.HasValue) return;
        if (!unit.IsAlive) return;

        Cell destCell = GridManager.Instance?.GetCell(targetCell.Value.x, targetCell.Value.y);
        if (destCell == null || !destCell.isWalkable) return;

        if (requirePath)
        {
            var path = context.cachedPath;
            if (path == null || path.Count == 0)
                path = GridManager.Instance.FindPath(unit.GridPosition, targetCell.Value);
            if (path == null || path.Count == 0) return;
            context.cachedPath = path;
        }
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        Unit unit = context.GetExecutorUnit();
        Vector2Int? targetCell = context.GetExecutedCell();
        if (unit == null || !targetCell.HasValue) yield break;

        var path = context.cachedPath;
        yield return unit.MoveTo(targetCell.Value, path);

        // 移动完成 → 回到待机
        var appearance = unit.GetComponent<UnitAppearance>();
        if (appearance != null)
            appearance.SetIdle();
    }

    public override void OnComplete(EffectContext context)
    {
        if (clearPoints)
        {
            Unit unit = context.GetExecutorUnit();
            if (unit != null)
                unit.AttributeManager.SetBaseValue(AttributeType.MovePoints, 0);
        }
    }
}