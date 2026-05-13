using UnityEngine;

[CreateAssetMenu(fileName = "DefenseStanceBuff", menuName = "Buffs/DefenseStanceBuff")]
public class DefenseStanceBuff : Buff
{
    public float damageReduction = 0.5f;

    public override void OnApply(Character target)
    {
        target.attributeManager.AddModifier(AttributeType.DamageReduction, new Modifier(this, damageReduction, ModifierType.Multiply));
    }

    public override void OnRemove(Character target)
    {
        target.attributeManager.RemoveModifiersFromSource(this);
    }
}