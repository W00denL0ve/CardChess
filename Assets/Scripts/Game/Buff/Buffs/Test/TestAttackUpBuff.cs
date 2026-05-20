using UnityEngine;

[CreateAssetMenu(menuName = "CardChess/Buffs/Test/AttackUp")]
public class TestAttackUpBuff : Buff
{
    public int attackBonus = 5;

    public override void OnApply(BuffInstance instance)
    {
        var mod = new Modifier(this, attackBonus, ModifierType.Add);
        AddModifier(instance, AttributeType.Attack, mod);
    }

    public override void OnRemove(BuffInstance instance)
    {
        var mod = instance.FindModifier(AttributeType.Attack, ModifierType.Add, this);
        if (mod != null)
            RemoveModifier(instance, AttributeType.Attack, mod);
    }
}