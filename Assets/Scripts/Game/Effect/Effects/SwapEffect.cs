using UnityEngine;

/// <summary>
/// 交换效果 - 将 context.executor 与 context.executed 交换位置
/// </summary>
[CreateAssetMenu(fileName = "SwapEffect", menuName = "Game/Effect/Swap")]
public class SwapEffect : Effect
{
    public override void OnExecute(EffectContext context)
    {
        Unit execUnit = context.GetExecutorUnit();
        Unit execdUnit = context.GetExecutedUnit();
        if (execUnit == null || execdUnit == null) return;
        if (!execUnit.IsAlive || !execdUnit.IsAlive) return;

        Vector2Int posA = execUnit.GridPosition;
        Vector2Int posB = execdUnit.GridPosition;

        execUnit.RequestMove(posB, context);
        execdUnit.RequestMove(posA, context);
    }
}
