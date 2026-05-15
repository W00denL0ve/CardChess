using UnityEngine;

/// <summary>
/// 效果上下文 - 携带效果执行所需的所有运行时信息
/// </summary>
[System.Serializable]
public struct EffectContext
{
    /// <summary>施法者 GameObject（通常挂有 Unit 组件）</summary>
    public GameObject caster;

    /// <summary>第一个锚点（例如用户选中的单位）</summary>
    public ITarget anchor1;

    /// <summary>第二个锚点（例如用户选中的格子）</summary>
    public ITarget anchor2;

    /// <summary>来源卡牌（可选）</summary>
    public Card sourceCard;

    /// <summary>额外动态参数</summary>
    public object[] customParams;
}