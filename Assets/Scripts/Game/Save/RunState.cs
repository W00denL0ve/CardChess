using System.Collections.Generic;

/// <summary>
/// 单个单位的存档数据 — assetAddress 指向静态模板，其余字段为运行时实例数据
/// </summary>
[System.Serializable]
public class UnitSaveData
{
    /// <summary>UnitConfig 的 Addressable 地址（静态模板）</summary>
    public string assetAddress;

    /// <summary>当前生命值（战斗结束时保存，用于跨关继承）</summary>
    public int currentHp;

    /// <summary>生命上限（可能因升级/遗物改变）</summary>
    public int maxHp;

    /// <summary>等级</summary>
    public int level = 1;

    /// <summary>经验值</summary>
    public int exp;

    public UnitSaveData(string assetAddress)
    {
        this.assetAddress = assetAddress;
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

    /// <summary>玩家阵容 — 存 UnitConfig 的 Addressable 地址</summary>
    public List<UnitSaveData> roster = new();

    // ── 持久化资源 ──

    /// <summary>能量上限（当前能量每局从上限重置，不存档）</summary>
    public int maxEnergy = 6;

    /// <summary>金币</summary>
    public int gold = 0;

    /// <summary>牌库中卡牌的 Addressable 地址列表</summary>
    public List<string> deckCardIds = new();
}
