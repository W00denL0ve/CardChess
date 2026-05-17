using UnityEngine;

/// <summary>
/// 伤害效果 - 对 context.executed 单位造成伤害
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Effect/Damage")]
public class DamageEffect : Effect
{
    /// <summary>伤害数值</summary>
    public int damageAmount;
    public DamageType damageType = DamageType.Physical;

    public override void OnExecute(EffectContext context)
    {
        Unit unit = context.GetExecutedUnit();
        if (unit == null) return;

        int defense = unit.GetDefenseFor(damageType);
        int finalDamage = Mathf.Max(1, damageAmount - defense);
        unit.TakeDamage(finalDamage, context);
    }
}