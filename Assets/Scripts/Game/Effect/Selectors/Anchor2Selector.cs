using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 直接返回 context.anchor2（如果非空）
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/Anchor2")]
public class Anchor2Selector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        return context.anchor2 == null
            ? new List<ITarget>()
            : new List<ITarget> { context.anchor2 };
    }
}
