using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手动单位选择器 — 在效果执行前需要玩家从候选列表中选择一个单位
/// 由 AsyncEffectExecutor 手动解析，GetTargets() 在此路径不生效
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/ManualUnit")]
public class ManualUnitSelector : TargetSelector
{
    public enum CandidateType
    {
        Enemies,
        Allies,
        All,
        SameFaction
    }

    [Header("候选范围")]
    public CandidateType candidateType = CandidateType.Enemies;

    [Tooltip("当 candidateType = SameFaction 时使用此阵营")]
    public Faction targetFaction = Faction.Neutral;

    /// <summary>
    /// 手动选择器在 GetTargets 中不返回结果（由 AsyncEffectExecutor 异步解析）
    /// </summary>
    public override List<ITarget> GetTargets(EffectContext context)
    {
        return new List<ITarget>();
    }
}
