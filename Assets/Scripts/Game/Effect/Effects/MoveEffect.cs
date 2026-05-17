using UnityEngine;

/// <summary>
/// 移动效果 - 将 context.executor 移动到 context.executed 所在格子
/// </summary>
[CreateAssetMenu(fileName = "MoveEffect", menuName = "Game/Effect/Move")]
public class MoveEffect : Effect
{
    /// <summary>是否需要路径可达</summary>
    public bool requirePath = true;

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
            var path = GridManager.Instance.FindPath(executorUnit.GridPosition, targetCell.Value);
            if (path == null) return;
        }

        executorUnit.RequestMove(targetCell.Value, context);
    }
}