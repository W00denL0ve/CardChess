/// <summary>攻击位置修正器</summary>
public interface IAttackPositionModifier
{
    /// <summary>作为攻击者修正攻击方位</summary>
    AttackPosition ModifyAttackPosition(AttackPosition currentPosition);

    /// <summary>作为受击者修正受击方位</summary>
    AttackPosition ModifyHitPosition(AttackPosition currentPosition);
}
