using System;

/// <summary>
/// 效果链步骤基类 — 每步只能选择一种类型
/// </summary>
[Serializable]
public abstract class ChainStep { }

/// <summary>
/// 选择器步骤
/// </summary>
[Serializable]
public class SelectorStep : ChainStep
{
    public TargetSelector selector;
}

/// <summary>
/// 条件步骤 — 不满足则中断整条链
/// </summary>
[Serializable]
public class ConditionStep : ChainStep
{
    public Condition condition;
}

/// <summary>
/// 效果步骤
/// </summary>
[Serializable]
public class EffectStep : ChainStep
{
    public Effect effect;
}
