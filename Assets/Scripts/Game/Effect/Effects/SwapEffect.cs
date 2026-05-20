using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交换效果 - 将 context.executor 与 context.executed 交换位置
/// </summary>
[CreateAssetMenu(fileName = "SwapEffect", menuName = "CardChess/EffectChain/Effects/Swap")]
public class SwapEffect : Effect, IAnimatedEffect
{
    // 在 OnExecute 中记录位置，供 PlayAnimation 使用
    private Vector2Int posA;
    private Vector2Int posB;
    private Unit execUnit;
    private Unit execdUnit;

    public override void OnExecute(EffectContext context)
    {
        execUnit = context.GetExecutorUnit();
        execdUnit = context.GetExecutedUnit();
        if (execUnit == null || execdUnit == null) return;
        if (!execUnit.IsAlive || !execdUnit.IsAlive) return;

        posA = execUnit.GridPosition;
        posB = execdUnit.GridPosition;
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        if (execUnit == null || execdUnit == null) yield break;

        // 先移动 executor → executed 的位置
        List<Vector2Int> pathAB = GridManager.Instance?.FindPath(execUnit.GridPosition, posB);
        yield return execUnit.MoveTo(posB, pathAB);

        // 再移动 executed → executor 的原位置（此时格子已空出）
        List<Vector2Int> pathBA = GridManager.Instance?.FindPath(execdUnit.GridPosition, posA);
        yield return execdUnit.MoveTo(posA, pathBA);
    }

    public override void OnComplete(EffectContext context)
    {
        // 交换不清理行动力
    }
}
