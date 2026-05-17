using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 返回施法者的所有敌人（忽略锚点）
/// 注意：需要场景中存在 LevelManager 并提供 GetEnemiesOf 方法
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/AllEnemies")]
public class AllEnemiesSelector : TargetSelector
{
    public override List<ITarget> GetTargets(EffectContext context)
    {
        Unit execUnit = context.GetExecutorUnit();
        if (execUnit == null) return new List<ITarget>();

        if (LevelManager.Instance != null)
        {
            var enemies = LevelManager.Instance.GetEnemiesOf(execUnit);
            return enemies.Select(e => new UnitTarget(e)).Cast<ITarget>().ToList();
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
