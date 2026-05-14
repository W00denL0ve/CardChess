using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 效果基类 - 采用双选择器设计，通过 selectorA 和 selectorB 分别选择两组目标，
/// 再根据 combination 模式配对，对每对目标执行 ApplyToPair。
/// </summary>
public abstract class Effect : ScriptableObject
{
    [Header("目标选择")]
    [SerializeField] private TargetSelector selectorA;   // 选择"执行者"或"源"
    [SerializeField] private TargetSelector selectorB;   // 选择"承受者"或"目标"
    [SerializeField] private TargetCombination combination = TargetCombination.CrossProduct;

    [Header("效果基本信息")]
    public string effectName;
    public Sprite icon;

    /// <summary>
    /// 外部调用入口：根据选择器获取目标列表，按组合模式配对后执行具体效果
    /// </summary>
    public void Apply(EffectContext context)
    {
        List<ITarget> targetsA = selectorA?.GetTargets(context) ?? new List<ITarget>();
        List<ITarget> targetsB = selectorB?.GetTargets(context) ?? new List<ITarget>();

        if (targetsA.Count == 0 || targetsB.Count == 0)
        {
            Debug.LogWarning($"[Effect] {effectName} 选择器返回空目标列表 (A:{targetsA.Count}, B:{targetsB.Count})");
            return;
        }

        switch (combination)
        {
            case TargetCombination.CrossProduct:
                foreach (var a in targetsA)
                    foreach (var b in targetsB)
                        ApplyToPair(a, b, context);
                break;

            case TargetCombination.Zip:
                int count = Mathf.Min(targetsA.Count, targetsB.Count);
                for (int i = 0; i < count; i++)
                    ApplyToPair(targetsA[i], targetsB[i], context);
                break;

            case TargetCombination.FirstOfA_AllB:
                var firstA = targetsA[0];
                foreach (var b in targetsB)
                    ApplyToPair(firstA, b, context);
                break;

            case TargetCombination.AllA_FirstOfB:
                var firstB = targetsB[0];
                foreach (var a in targetsA)
                    ApplyToPair(a, firstB, context);
                break;
        }
    }

    /// <summary>
    /// 子类实现：对一对目标执行具体效果
    /// </summary>
    /// <param name="source">源目标（selectorA的结果）</param>
    /// <param name="target">目标（selectorB的结果）</param>
    /// <param name="context">效果上下文</param>
    protected abstract void ApplyToPair(ITarget source, ITarget target, EffectContext context);
}