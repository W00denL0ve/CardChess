using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "PoisonBuff", menuName = "CardChess/Buffs/Poison")]
public class PoisonBuff : Buff, IOnTurnEnd
{
    public void OnTurnEnd(BuffInstance instance)
    {
        instance.Host.TakeDamage(instance.CurrentStacks);
        instance.RemoveStack();
    }
}