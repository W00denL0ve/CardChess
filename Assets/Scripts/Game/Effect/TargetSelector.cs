using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 目标选择器基类 - 负责从 EffectContext 中计算出一组目标
/// </summary>
public abstract class TargetSelector : ScriptableObject
{
    public string selectorName;

    /// <summary>为 true 时 executor←old executed；为 false 时 executor 不变</summary>
    public bool changesExecutor = true;

    public abstract List<ITarget> GetTargets(EffectContext context);

    public virtual void PreviewHighlight(EffectContext context, bool show) { }
}