using UnityEngine;

[CreateAssetMenu(fileName = "DefenseStanceBuff", menuName = "CardChess/Buffs/DefenseStance")]
public class DefenseStanceBuff : Buff
{
    public float damageReduction = 0.5f;

    public override void OnApply(BuffInstance instance)
    {
        var mod = new Modifier(this, damageReduction, ModifierType.Multiply);
        AddModifier(instance, AttributeType.PhysicalDefense, mod);
    }

    public override void OnRemove(BuffInstance instance)
    {
        // Modifiers are cleaned up by BuffInstance.Cleanup()
    }
}