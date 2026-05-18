using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可复用的单位生成池 — 按稀有度 + 权重随机抽取
/// </summary>
[CreateAssetMenu(menuName = "Game/Unit/SpawnGroup")]
public class SpawnGroup : ScriptableObject
{
    public List<WeightedEntry> entries = new();

    /// <summary>
    /// 根据当前难度从池中抽取一个单位
    /// </summary>
    /// <param name="difficulty">综合难度值</param>
    /// <param name="runConfig">RunConfig（提供稀有度倍率曲线）</param>
    public UnitConfig PickUnit(float difficulty, RunConfig runConfig)
    {
        if (entries == null || entries.Count == 0) return null;
        if (runConfig == null) return entries[0].unitConfig;

        float totalWeight = 0;
        float[] adjustedWeights = new float[entries.Count];

        for (int i = 0; i < entries.Count; i++)
        {
            float multiplier = runConfig.GetMultiplier(entries[i].rarity, difficulty);
            if (multiplier <= 0f)
            {
                adjustedWeights[i] = 0;
                continue;
            }
            adjustedWeights[i] = entries[i].weight * multiplier;
            totalWeight += adjustedWeights[i];
        }

        if (totalWeight <= 0f) return entries[0].unitConfig;

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < entries.Count; i++)
        {
            if (adjustedWeights[i] <= 0f) continue;
            roll -= adjustedWeights[i];
            if (roll <= 0f) return entries[i].unitConfig;
        }

        return entries[entries.Count - 1].unitConfig;
    }
}
