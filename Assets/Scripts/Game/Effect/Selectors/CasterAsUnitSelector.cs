using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 将施法者自身包装为单位目标返回
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CasterAsUnit")]
public class CasterAsUnitSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        Unit unit = context.GetExecutorUnit();
        return unit != null
            ? new List<ITarget> { new UnitTarget(unit) }
            : new List<ITarget>();
    }

    public override void PreviewHighlight(EffectContext context, bool show)
    {
        var vis = Object.FindObjectOfType<UnitVisualizer>();
        if (vis == null) return;

        if (show)
        {
            var targets = GetTargets(context);
            var units = targets.Select(t => (t as UnitTarget)?.unit).Where(u => u != null).ToList();
            if (units.Count > 0) vis.HighlightUnits(units);
        }
        else
        {
            vis.ClearHighlights();
        }
    }
}
