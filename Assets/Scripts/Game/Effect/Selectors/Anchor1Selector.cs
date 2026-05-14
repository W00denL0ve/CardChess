using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 直接返回 context.anchor1（如果非空）
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/Anchor1")]
public class Anchor1Selector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        return context.anchor1 == null
            ? new List<ITarget>()
            : new List<ITarget> { context.anchor1 };
    }
}
