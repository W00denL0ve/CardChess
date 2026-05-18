using UnityEngine;

/// <summary>
/// 条件基类 — 检查是否满足某个条件
/// 不满足时，当前步骤的 effect 不执行，且整条链中断
/// </summary>
public abstract class Condition : ScriptableObject
{
    /// <summary>检查条件是否满足（context 已包含 selector 更新后的状态）</summary>
    public abstract bool IsMet(EffectContext context);
}
