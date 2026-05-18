using System;

/// <summary>
/// 带权重的单位生成条目
/// </summary>
[Serializable]
public struct WeightedEntry
{
    public UnitConfig unitConfig;
    public RarityTier rarity;
    public int weight;
}
