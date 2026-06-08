using UnityEngine;

[CreateAssetMenu(fileName = "DefenseStanceBuff", menuName = "CardChess/Buffs/DefenseStance")]
public class DefenseStanceBuff : Buff, IOnApplyBuff, IOnRemoveBuff
{
    public float damageReduction = 0.8f;

    public void OnApply(BuffInstance instance)
    {
        instance.AddModifier(damageReduction, ModifierType.FinalMultiply, ModifierField.PhysicalDefense);
    }

    public void OnRemove(BuffInstance instance)
    {
        instance.Cleanup();
    }
}