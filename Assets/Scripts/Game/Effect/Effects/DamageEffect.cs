using UnityEngine;

/// <summary>
/// 伤害效果 - 对目标造成伤害（忽略 source，只使用 target）
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Effect/Damage")]
public class DamageEffect : Effect
{
    /// <summary>伤害数值</summary>
    public int damageAmount;

    protected override void ApplyToPair(ITarget source, ITarget target, EffectContext context)
    {
        // 只关心 target 是否是单位
        UnitTarget unitTarget = target as UnitTarget;
        if (unitTarget?.unit == null) return;

        Character character = unitTarget.unit.GetComponent<Character>();
        if (character != null)
        {
            character.TakeDamage(damageAmount);
        }
    }
}