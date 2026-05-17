using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 返回施法者的所有友方单位（忽略锚点）
/// 注意：需要场景中存在 LevelManager 并提供 GetAlliesOf 方法
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/AllAllies")]
public class AllAlliesSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        Unit execUnit = context.GetExecutorUnit();
        if (execUnit == null) return new List<ITarget>();

        if (LevelManager.Instance != null)
        {
            var allies = LevelManager.Instance.GetAlliesOf(execUnit);
            return allies.Select(a => new UnitTarget(a)).Cast<ITarget>().ToList();
        }

        return new List<ITarget>();
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
