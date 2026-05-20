using System.Collections.Generic;
using UnityEngine;

// ====================================================================
//  阶段配置
// ====================================================================

/// <summary>阶段内包含的地图</summary>
[System.Serializable]
public class MapConfig
{
    public string id;
    public float difficultyModifier;
    public List<string> scenes = new();
}

/// <summary>阶段（如"新手村""野外"）</summary>
[System.Serializable]
public class PhaseConfig
{
    public string phaseName;
    public int levelCount;
    public float difficultyOffset;
    public List<MapConfig> mapPool = new();
}

// ====================================================================
//  RunConfig — 全局一局游戏的配置资产
// ====================================================================

/// <summary>
/// 全局一局游戏的完整配置 — 包含难度曲线、阶段列表、局内参数
/// </summary>
[CreateAssetMenu(menuName = "CardChess/Configs/RunConfig")]
public class RunConfig : ScriptableObject
{
    // ── 难度曲线（原 DifficultyConfig） ──

    [Header("各稀有度的权重倍率曲线（X=综合难度值, Y=倍率）")]
    public AnimationCurve commonMultiplier   = AnimationCurve.Constant(0f, 3f, 1f);
    public AnimationCurve uncommonMultiplier = AnimationCurve.Constant(0f, 3f, 1f);
    public AnimationCurve rareMultiplier     = new(new Keyframe(0f, 0f), new Keyframe(1f, 0.5f), new Keyframe(2f, 1.5f), new Keyframe(3f, 3f));
    public AnimationCurve eliteMultiplier    = new(new Keyframe(0f, 0f), new Keyframe(1.5f, 0.3f), new Keyframe(2.5f, 1.5f), new Keyframe(3f, 3f));

    [Header("稀有度解锁难度门槛")]
    public float rareMinDifficulty  = 0.5f;
    public float eliteMinDifficulty = 1.5f;

    [Header("阶段内的难度跨度")]
    public float stageRange = 1.0f;

    // ── 阶段列表 ──

    [Header("阶段列表")]
    public List<PhaseConfig> phases = new();

    // ── 局内参数 ──

    [Header("局内参数")]
    public int maxRosterSize = 3;
    public bool reviveEnabled = true;

    // ── 方法 ──

    /// <summary>获取指定稀有度在给定难度下的权重倍率</summary>
    public float GetMultiplier(RarityTier rarity, float difficulty)
    {
        return rarity switch
        {
            RarityTier.Common    => commonMultiplier.Evaluate(difficulty),
            RarityTier.Uncommon  => uncommonMultiplier.Evaluate(difficulty),
            RarityTier.Rare      => difficulty >= rareMinDifficulty ? rareMultiplier.Evaluate(difficulty) : 0f,
            RarityTier.Elite     => difficulty >= eliteMinDifficulty ? eliteMultiplier.Evaluate(difficulty) : 0f,
            _ => 1f
        };
    }

    /// <summary>根据全局关卡进度 + 地图配置计算综合难度</summary>
    public float CalculateDifficulty(int globalStageIndex, string mapId)
    {
        int accumulated = 0;
        foreach (var phase in phases)
        {
            if (accumulated + phase.levelCount > globalStageIndex)
            {
                int localIndex = globalStageIndex - accumulated;
                float mapMod = 0f;
                foreach (var map in phase.mapPool)
                {
                    if (map.id == mapId)
                    {
                        mapMod = map.difficultyModifier;
                        break;
                    }
                }
                return phase.difficultyOffset + mapMod + (localIndex / (float)phase.levelCount) * stageRange;
            }
            accumulated += phase.levelCount;
        }
        return 0f;
    }
}
