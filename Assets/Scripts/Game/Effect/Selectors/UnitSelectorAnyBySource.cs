using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 单位选择器 — 依据被执行者 + OR 筛选
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/UnitAnyBySource")]
public class UnitSelectorAnyBySource : TargetSelector
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
    public int maxRange = 0;

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
        var result = new HashSet<Unit>();
        var factionMask = (int)factions;
        var occMask = (int)occupations;
        bool hasNameFilter = !string.IsNullOrEmpty(nameFilter);

        foreach (var u in all)
        {
            // 包含
            if (u == execUnit && !includeSelf) continue;
            if (u != execUnit && !includeOthers) continue;

            // 范围
            int dist = Mathf.Abs(u.GridPosition.x - center.x) + Mathf.Abs(u.GridPosition.y - center.y);
            if (maxRange > 0 && (dist < minRange || dist > maxRange)) continue;

            // OR
            bool matched = false;
            if ((factionMask & (1 << (int)u.Faction)) != 0) matched = true;
            if (!matched && (occMask & (1 << (int)u.Occupation)) != 0) matched = true;
            if (!matched && hasNameFilter && MatchWildcard(u.UnitId ?? u.name, nameFilter)) matched = true;

            if (matched) result.Add(u);
        }

        return result.Select(u => (ITarget)new UnitTarget(u)).ToList();
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
