using System;

/// <summary>
/// 卡牌效果链的一个步骤 — 选择器 或/和 效果的组合
/// selector 和 effect 各自可选：见到选择器则更新上下文，见到效果则执行
/// </summary>
[Serializable]
public class GameEffectStep
{
    /// <summary>目标选择器（可选，为 null 则跳过选择阶段）</summary>
    public TargetSelector selector;

    /// <summary>效果资产（可选，为 null 则跳过执行阶段）</summary>
    public Effect effect;
}
