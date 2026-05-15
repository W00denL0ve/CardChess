using UnityEngine;

/// <summary>
/// 伤害效果 - 对目标造成伤害（忽略 source，只使用 target）
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Effect/Damage")]
public class DamageEffect : Effect
{
    /// <summary>伤害数值</summary>
    public int damageAmount;
    public DamageType damageType = DamageType.Physical;

    protected override void ApplyToPair(ITarget source, ITarget target, EffectContext context)
    {
        UnitTarget unitTarget = target as UnitTarget;
        if (unitTarget?.unit == null) return;

        int defense = unitTarget.unit.GetDefenseFor(damageType);
        int finalDamage = Mathf.Max(1, damageAmount - defense);
        unitTarget.unit.TakeDamage(finalDamage, context);
    }
}