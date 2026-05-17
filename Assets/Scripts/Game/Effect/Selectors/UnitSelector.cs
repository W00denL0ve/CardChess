using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 单位选择器 — 按条件筛选单位
/// 条件精确到只有 1 个匹配时自动选择，无需玩家干预
/// 用于效果链的第一个步骤（与被执行者无关）
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/UnitSelector")]
public class UnitSelector : TargetSelector
{
    public enum CountMode { Single, All, Count }

    public enum FactionFilter { Any, Player, Enemy, Neutral }
    public enum OccupationFilter { Any, Warrior, Rogue, Mage }

    [Header("选择数量")]
    public CountMode countMode = CountMode.Single;
    [Tooltip("当 countMode = Count 时使用")]
    public int customCount = 3;

    [Header("过滤条件（留空/Any = 不限）")]
    public FactionFilter factionFilter = FactionFilter.Any;
    public OccupationFilter occupationFilter = OccupationFilter.Any;
    public string nameFilter = "";

    [Header("其他")]
    [Tooltip("排除 context.executor 自身")]
    public bool excludeExecutor = false;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        var lm = LevelManager.Instance;
        if (lm == null) return new List<ITarget>();

        var allUnits = lm.AllUnits.Where(u => u.IsAlive);

        // 阵营过滤
        if (factionFilter != FactionFilter.Any)
        {
            var targetFaction = (Faction)(int)factionFilter - 1; // Player=0→Player, Enemy=1→Enemy
            allUnits = allUnits.Where(u => u.Faction == targetFaction);
        }

        // 职业过滤
        if (occupationFilter != OccupationFilter.Any)
        {
            var targetOcc = (Occupation)(int)occupationFilter - 1;
            allUnits = allUnits.Where(u => u.Occupation == targetOcc);
        }

        // 名称过滤
        if (!string.IsNullOrEmpty(nameFilter))
            allUnits = allUnits.Where(u => u.UnitId == nameFilter || u.name == nameFilter);

        // 排除执行者
        if (excludeExecutor)
        {
            Unit execUnit = context.GetExecutorUnit();
            if (execUnit != null)
                allUnits = allUnits.Where(u => u != execUnit);
        }

        var result = allUnits.ToList();

        // 根据 countMode 截取
        int takeCount;
        switch (countMode)
        {
            case CountMode.Single: takeCount = 1; break;
            case CountMode.All: takeCount = result.Count; break;
            case CountMode.Count: takeCount = Mathf.Min(customCount, result.Count); break;
            default: takeCount = 1; break;
        }

        result = result.Take(takeCount).ToList();
        return result.Select(u => (ITarget)new UnitTarget(u)).ToList();
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
