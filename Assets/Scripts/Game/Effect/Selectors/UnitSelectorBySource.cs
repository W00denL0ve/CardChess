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
    public enum FactionMask { Ally = 1, Hostile = 2, Neutral = 4, Self = 8 }
    [System.Flags]
    public enum OccMask { Warrior = 1, Rogue = 2, Mage = 4 }

    [Header("范围（执行者为中心）")]
    public int minRange = 0;
    public int maxRange = 0; // 0=不限

    [Header("阵营关系（多选）")]
    public FactionMask factions = (FactionMask)0;
    [Header("职业（多选）")]
    public OccMask occupations = (OccMask)0;
    [Header("ID（通配）")]
    public string IDFilter;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        var lm = LevelManager.Instance;
        Unit execUnit = context.GetExecutorUnit();      // 执行者

        if (lm == null || execUnit == null || !execUnit.IsAlive)
            return new List<ITarget>();

        var center = execUnit.GridPosition;
        var candidates = new HashSet<Unit>();           // 用HashSet自动去重

        // 1. 根据阵营掩码收集单位
        if (factions.HasFlag(FactionMask.Self))
            candidates.Add(execUnit);

        if (factions.HasFlag(FactionMask.Ally))
        {
            var allies = lm.GetAlliesOf(execUnit, includeSelf: false);  // 不包含自己，避免重复
            foreach (var u in allies) candidates.Add(u);
        }

        if (factions.HasFlag(FactionMask.Hostile))
        {
            var enemies = lm.GetEnemiesOf(execUnit, includeNeutral: false);
            foreach (var u in enemies) candidates.Add(u);
        }

        if (factions.HasFlag(FactionMask.Neutral))
        {
            // 当明确要求中立单位时（且没有Hostile），单独获取中立
            var neutrals = lm.GetUnitsByFaction(Faction.Neutral);
            foreach (var u in neutrals) candidates.Add(u);
        }

        // 2. 职业筛选
        if (occupations != 0)
        {
            candidates.RemoveWhere(u =>
            {
                OccMask unitMask = 0;
                switch (u.Occupation)   // 假设 Unit 有 Occupation 属性
                {
                    case Occupation.Warrior: unitMask = OccMask.Warrior; break;
                    case Occupation.Rogue:  unitMask = OccMask.Rogue;  break;
                    case Occupation.Mage:   unitMask = OccMask.Mage;   break;
                    default: return true;   // 未知职业，排除
                }
                return (occupations & unitMask) == 0;   // 不匹配则移除
            });
        }

        // 3. 名称通配符筛选
        if (!string.IsNullOrEmpty(IDFilter))
        {
            candidates.RemoveWhere(u => !MatchWildcard(u.UnitId, IDFilter));
        }

        // 4. 范围筛选（曼哈顿距离）
        candidates.RemoveWhere(u =>
        {
            int dist = Mathf.Abs(u.GridPosition.x - center.x) + Mathf.Abs(u.GridPosition.y - center.y);
            if (maxRange > 0 && (dist < minRange || dist > maxRange)) return true;
            if (minRange > 0 && dist < minRange) return true;
            return false;
        });

        // 5. 转换为 ITarget 列表
        return candidates.Select(u => (ITarget)new UnitTarget(u)).ToList();
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
