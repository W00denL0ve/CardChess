using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 目标选择器基类 - 负责从 EffectContext 中计算出一组目标
/// </summary>
public abstract class TargetSelector : ScriptableObject
{
    /// <summary>选择器名称（可视化用）</summary>
    public string selectorName;

    /// <summary>
    /// 核心方法：根据上下文返回目标列表
    /// </summary>
    public abstract List<ITarget> GetTargets(EffectContext context);
}