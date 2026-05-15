using UnityEngine;

/// <summary>
/// 添加 Buff 效果 - 为目标单位添加一个 Buff
/// </summary>
[CreateAssetMenu(fileName = "AddBuffEffect", menuName = "Game/Effect/AddBuff")]
public class AddBuffEffect : Effect
{
    /// <summary>要添加的 Buff</summary>
    public Buff buff;

    /// <summary>持续回合数</summary>
    public int duration;

    protected override void ApplyToPair(ITarget source, ITarget target, EffectContext context)
    {
        UnitTarget unitTarget = target as UnitTarget;
        if (unitTarget?.unit == null) return;

        unitTarget.unit.BuffContainer.ApplyBuff(buff, context);
    }
}