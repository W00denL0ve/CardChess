using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将施法者自身包装为单位目标返回
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CasterAsUnit")]
public class CasterAsUnitSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        if (context.caster == null) return new List<ITarget>();
        Unit unit = context.caster.GetComponent<Unit>();
        return unit != null
            ? new List<ITarget> { new UnitTarget(unit) }
            : new List<ITarget>();
    }
}
