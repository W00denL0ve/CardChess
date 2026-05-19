using System.Collections.Generic;

/// <summary>
/// 单个单位的存档数据
/// </summary>
[System.Serializable]
public class UnitSaveData
{
    /// <summary>UnitConfig 的标识（用于查找资产）</summary>
    public string configId;

    public UnitSaveData(string configId)
    {
        this.configId = configId;
    }
}

/// <summary>
/// 一局游戏的完整运行时状态 — JSON 序列化/反序列化
/// 所有需要跨关保留的数据都集中在这里
/// </summary>
[System.Serializable]
public class RunState
{
    /// <summary>存档版本号（用于版本迁移）</summary>
    public int version = 1;

    /// <summary>当前已推进到第几关（从 1 开始）</summary>
    public int globalStageIndex = 1;

    /// <summary>随机种子</summary>
    public int randomSeed;

    /// <summary>玩家阵容 — 存关卡的 UnitConfig 标识</summary>
    public List<UnitSaveData> roster = new();
}
