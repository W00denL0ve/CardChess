using UnityEngine;

/// <summary>
/// 效果上下文 — 按效果链的步骤顺序传递
/// 初始 executor = executed = 卡牌发出者
/// 每经过一个目标选择器：executor ← executed, executed ← 目标
/// </summary>
public struct EffectContext
{
    /// <summary>来源卡牌</summary>
    public CardData sourceCard;

    /// <summary>当前步骤的执行者（上一个步骤的被执行者）</summary>
    public ITarget executor;

    /// <summary>当前步骤的被执行者</summary>
    public ITarget executed;

    /// <summary>额外动态参数</summary>
    public object[] customParams;

    /// <summary>便捷方法：获取执行者的 Unit 组件</summary>
    public Unit GetExecutorUnit() => executor?.gameObject?.GetComponent<Unit>();

    /// <summary>便捷方法：获取被执行者的 Unit 组件</summary>
    public Unit GetExecutedUnit() => executed?.gameObject?.GetComponent<Unit>();

    /// <summary>便捷方法：获取被执行者的格子坐标</summary>
    public Vector2Int? GetExecutedCell() => executed?.GetCellPosition();
}