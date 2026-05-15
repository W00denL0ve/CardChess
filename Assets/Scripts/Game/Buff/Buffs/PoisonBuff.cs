using UnityEngine;

[CreateAssetMenu(fileName = "PoisonBuff", menuName = "Buffs/PoisonBuff")]
public class PoisonBuff : Buff
{
    public float damagePerTurn = 10f;

    public override void OnTurnEnd(BuffInstance instance)
    {
        instance.Host.TakeDamage((int)damagePerTurn, instance.SourceContext);
    }
}