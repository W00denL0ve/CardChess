using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 效果上下文 — 引用类型，效果链中所有步骤共享同一实例
/// 初始 executor = executed = 卡牌发出者
/// 每经过一个目标选择器：executor ← executed, executed ← 目标
///
/// cachedPath 用于在选择器与效果之间传递路径数据
/// （选择器计算路径后写入，效果消费，执行完毕后清空）
/// </summary>
public class EffectContext
{
    /// <summary>来源卡牌</summary>
    public CardData sourceCard;

    /// <summary>当前步骤的执行者（上一个步骤的被执行者）</summary>
    public ITarget executor;

    /// <summary>当前步骤的被执行者</summary>
    public ITarget executed;

    /// <summary>额外动态参数</summary>
    public object[] customParams;

    /// <summary>选择器到效果之间传递的缓存路径（如 BFS 寻路结果）</summary>
    public List<Vector2Int> cachedPath;

    /// <summary>当前步骤是否可以回退到上一个选择器（由 Executor 维护）</summary>
    public bool canRevert;

    /// <summary>内部标志：链是否应中断（由 ConditionStep 设置，Executor 检查）</summary>
    internal bool chainBroken;

    /// <summary>便捷方法：获取执行者的 Unit 组件</summary>
    public Unit GetExecutorUnit() => executor?.gameObject?.GetComponent<Unit>();

    /// <summary>便捷方法：获取被执行者的 Unit 组件</summary>
    public Unit GetExecutedUnit() => executed?.gameObject?.GetComponent<Unit>();

    /// <summary>便捷方法：获取被执行者的格子坐标</summary>
    public Vector2Int? GetExecutedCell() => executed?.GetCellPosition();

    /// <summary>清除选择器到效果之间的临时缓存数据</summary>
    public void ClearStepCache()
    {
        cachedPath = null;
        customParams = null;
        chainBroken = false;
    }
}