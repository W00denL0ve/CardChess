using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合行动抽象基类 - 所有回合中自动执行的行动都派生自此类
/// 使用 [SerializeReference] 实现多态序列化
/// </summary>
[System.Serializable]
public abstract class TurnAction
{
    /// <summary>格子逻辑坐标（0-based col/row，相对于基础网格原点）</summary>
    public Vector2Int coord;
}

/// <summary>
/// 单位生成行动 - 在指定格子生成单位
/// </summary>
[System.Serializable]
public class SpawnUnitAction : TurnAction
{
    /// <summary>随机生成池</summary>
    public SpawnGroup spawnGroup;

    /// <summary>兜底单位配置</summary>
    public UnitConfig fallbackUnitConfig;

    public Faction factionOverride;
    public bool useConfigFaction = true;
    public int count = 1;

    /// <summary>允许生成的地形类型（为空则不限制）</summary>
    public List<TerrainType> allowedTerrains;

    /// <summary>出生点被占用时的搜索半径（曼哈顿距离，0=不搜索）</summary>
    public int searchRange = 0;
}

/// <summary>
/// 格子变化行动 - 修改指定格子的地形属性（允许部分修改）
/// </summary>
[System.Serializable]
public class CellChangeAction : TurnAction
{
    /// <summary>新的地形类型（null 表示不修改）</summary>
    public TerrainType? newTerrainType;

    /// <summary>新的高度值（绝对值，null 表示不修改）</summary>
    public int? newHeight;

    /// <summary>是否可行走（null 表示不修改）</summary>
    public bool? setWalkable;
}

/// <summary>
/// 效果应用行动 - 在指定格子应用一个 Effect
/// todo 注意：应该采用效果链，不急于实现
/// </summary>
[System.Serializable]
public class EffectApplyAction : TurnAction
{
    /// <summary>要应用的效果资产</summary>
    public Effect effectToApply;
}
