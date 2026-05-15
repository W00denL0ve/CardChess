using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Buffs/Test/Poison")]
public class TestPoisonBuff : Buff
{
    public int poisonDamage = 5;

    public override void OnTurnEnd(BuffInstance instance)
    {
        // 使用 Buff 来源上下文作为伤害来源
        instance.Host.TakeDamage(poisonDamage, instance.SourceContext);
    }
}