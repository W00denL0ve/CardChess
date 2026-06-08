using UnityEngine;

public enum BuffStackStrategy
{
    Refresh, // 叠加并刷新持续时间
    Separate, // 独立计时计层
    Overwrite // 覆盖计时计层
}

/// <summary>
/// Buff 数据资产 —— 只包含数据字段，生命周期逻辑通过接口实现按需注入。
/// </summary>
public abstract class Buff : ScriptableObject
{
    [Tooltip("Buff 叠加策略")]
    public BuffStackStrategy stackStrategy;
    public string buffId;
    public Sprite icon;
    public string description;

    /// <summary><0 表示永久</summary>
    public int defaultDuration;
    public bool isDebuff;
    public int maxStack = 1;
}