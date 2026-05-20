using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 单位选择器 — 依据被执行者 + AND 筛选
/// </summary>
[CreateAssetMenu(menuName = "CardChess/EffectChain/Selectors/UnitBySource")]
public class UnitSelectorBySource : TargetSelector
{
    [System.Flags]
    public enum FactionMask { Player = 1, Enemy = 2, Neutral = 4 }
    [System.Flags]
    public enum OccMask { Warrior = 1, Rogue = 2, Mage = 4 }

    [Header("包含")]
    public bool includeSelf = true;
    public bool includeOthers = true;

    [Header("范围（曼哈顿距离，以被执行者为中心）")]
    public int minRange = 0;
    public int maxRange = 0; // 0=不限

    [Header("阵营（多选）")]
    public FactionMask factions = (FactionMask)7;
    [Header("职业（多选）")]
    public OccMask occupations = (OccMask)7;
    [Header("名称（通配）")]
    public string nameFilter;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        var lm = LevelManager.Instance;
        var execUnit = context.GetExecutedUnit();
        if (lm == null || execUnit == null) return new List<ITarget>();

        var all = lm.AllUnits.Where(u => u.IsAlive).ToList();
        var center = execUnit.GridPosition;

        var result = new List<Unit>();

        foreach (var u in all)
        {
            // 包含
            if (u == execUnit && !includeSelf) continue;
            if (u != execUnit && !includeOthers) continue;

            // 范围
            int dist = Mathf.Abs(u.GridPosition.x - center.x) + Mathf.Abs(u.GridPosition.y - center.y);
            if (maxRange > 0 && (dist < minRange || dist > maxRange)) continue;

            // AND 阵营
            if (((int)factions & (1 << (int)u.Faction)) == 0) continue;

            // AND 职业
            if (occupations != (OccMask)7 && ((int)occupations & (1 << (int)u.Occupation)) == 0) continue;

            // AND 名称
            if (!string.IsNullOrEmpty(nameFilter) && !MatchWildcard(u.UnitId ?? u.name, nameFilter)) continue;

            result.Add(u);
        }

        return result.Select(u => (ITarget)new UnitTarget(u)).ToList();
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
            if (parts.Length == 2) return input.StartsWith(parts[0]) && input.EndsWith(parts[1]);
            return input.Contains(parts[0]);
        }
        return input == pattern;
    }
}
