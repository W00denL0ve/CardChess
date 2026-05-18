using UnityEngine;

/// <summary>
/// 条件：被执行者已死亡
/// </summary>
[CreateAssetMenu(menuName = "Game/Condition/ExecutedIsDead")]
public class ExecutedIsDeadCondition : Condition
{
    public override bool IsMet(EffectContext context)
    {
        return context.GetExecutedUnit()?.IsAlive == false;
    }
}
