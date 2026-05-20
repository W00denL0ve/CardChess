using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 单位选择器 — 全图 AND 筛选
/// </summary>
[CreateAssetMenu(menuName = "CardChess/EffectChain/Selectors/Unit")]
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
        var alive = units.ToList();
        Logger.Log($"[UnitSelector] AllUnits={lm.AllUnits.Count}, 存活={alive.Count}, factions={(int)factions}, occ={(int)occupations}, nameFilter='{nameFilter}'");
        foreach (var u in alive)
            Logger.Log($"[UnitSelector]   单位: {u.UnitId} Faction={(int)u.Faction} Occ={(int)u.Occupation} IsAlive={u.IsAlive}");

        var afterFaction = alive.Where(u => (factions & (FactionMask)(1 << (int)u.Faction)) != 0).ToList();
        Logger.Log($"[UnitSelector] 阵营过滤后: {afterFaction.Count}");

        if (occupations != (OccMask)7)
            afterFaction = afterFaction.Where(u => (occupations & (OccMask)(1 << (int)u.Occupation)) != 0).ToList();

        if (!string.IsNullOrEmpty(nameFilter))
            afterFaction = afterFaction.Where(u => MatchWildcard(u.UnitId ?? u.name, nameFilter)).ToList();

        Logger.Log($"[UnitSelector] 最终结果: {afterFaction.Count}");
        return afterFaction.Select(u => (ITarget)new UnitTarget(u)).ToList();
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
