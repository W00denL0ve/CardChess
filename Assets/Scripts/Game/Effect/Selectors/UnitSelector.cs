using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 单位选择器 — 全图 AND 筛选
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/Unit")]
public class UnitSelector : TargetSelector
{
    [System.Flags]
    public enum FactionMask { Player = 1, Enemy = 2, Neutral = 4 }
    [System.Flags]
    public enum OccMask { Warrior = 1, Rogue = 2, Mage = 4 }

    [Header("阵营（多选）")]
    public FactionMask factions = (FactionMask)7; // 默认全选

    [Header("职业（多选）")]
    public OccMask occupations = (OccMask)7;

    [Header("名称（通配）")]
    public string nameFilter;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        var lm = LevelManager.Instance;
        if (lm == null) return new List<ITarget>();

        IEnumerable<Unit> units = lm.AllUnits.Where(u => u.IsAlive);

        units = units.Where(u => (factions & (FactionMask)(1 << (int)u.Faction)) != 0);

        if (occupations != (OccMask)7)
            units = units.Where(u => (occupations & (OccMask)(1 << (int)u.Occupation)) != 0);

        if (!string.IsNullOrEmpty(nameFilter))
            units = units.Where(u => MatchWildcard(u.UnitId ?? u.name, nameFilter));

        return units.Select(u => (ITarget)new UnitTarget(u)).ToList();
    }

    public override void PreviewHighlight(EffectContext context, bool show)
    {
        var vis = UnitVisualizer.Instance;
        if (vis == null) return;
        if (show)
        {
            var targets = GetTargets(context);
            var units = targets.Select(t => (t as UnitTarget)?.unit).Where(u => u != null).ToList();
            if (units.Count > 0) vis.HighlightUnits(units);
        }
        else { vis.ClearHighlights(); }
    }

    static bool MatchWildcard(string input, string pattern)
    {
        if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*');
            if (parts.Length == 2)
                return input.StartsWith(parts[0]) && input.EndsWith(parts[1]);
            return input.Contains(parts[0]);
        }
        return input == pattern;
    }
}
