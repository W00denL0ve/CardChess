using UnityEngine;

/// <summary>
/// 添加 Buff 效果 - 为 context.executed 单位添加一个 Buff
/// </summary>
[CreateAssetMenu(fileName = "AddBuffEffect", menuName = "CardChess/EffectChain/Effects/AddBuff")]
public class AddBuffEffect : Effect
{
    /// <summary>要添加的 Buff</summary>
    public Buff buff;

    public override void OnExecute(EffectContext context)
    {
        Unit unit = context.GetExecutedUnit();
        if (unit == null) return;

        unit.BuffContainer.ApplyBuff(buff, context);
    }
}