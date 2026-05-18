using UnityEngine;

/// <summary>
/// 移动效果 - 将 context.executor 移动到 context.executed 所在格子
/// 优先使用 context.cachedPath，其次重新计算
/// </summary>
[CreateAssetMenu(fileName = "MoveEffect", menuName = "Game/Effect/Move")]
public class MoveEffect : Effect
{
    /// <summary>是否需要路径可达</summary>
    public bool requirePath = true;

    [Tooltip("是否清空行动力")]
    public bool clearPoints = true;

    public override void OnExecute(EffectContext context)
    {
        Unit executorUnit = context.GetExecutorUnit();
        Vector2Int? targetCell = context.GetExecutedCell();
        if (executorUnit == null || !targetCell.HasValue) return;

        if (!executorUnit.IsAlive) return;

        Cell destCell = GridManager.Instance?.GetCell(targetCell.Value.x, targetCell.Value.y);
        if (destCell == null || !destCell.isWalkable) return;

        if (requirePath)
        {
            // 优先使用上下文缓存的路径
            var path = context.cachedPath;
            if (path == null || path.Count == 0)
                path = GridManager.Instance.FindPath(executorUnit.GridPosition, targetCell.Value);

            if (path == null || path.Count == 0) return;
        }

        executorUnit.RequestMove(targetCell.Value, context, clearPoints );
    }
}