/// <summary>
/// 伤害修改事件（可变引用类型，订阅者可修改 Damage 值）
/// 用于 Unit.BeforeDamage / Unit.AfterDamage 单位本地事件
/// </summary>
public class DamageModifyEvent
{
    public int Damage;
    public EffectContext Context;
}

/// <summary>
/// 攻击方位修改事件
/// 用于 BuffContainer.ModifyAttackPosition / ModifyHitPosition
/// </summary>
public class AttackPosEvent
{
    public AttackPosition Position;
}
