using UnityEngine;

[CreateAssetMenu(fileName = "AddBuffEffect", menuName = "Effects/AddBuffEffect")]
public class AddBuffEffect : Effect
{
    public Buff buff;
    public int duration;

    public override void Execute(EffectContext context)
    {
        // context.target.AddBuff(buff, duration);
    }
}