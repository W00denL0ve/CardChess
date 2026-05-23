using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 目标选择器基类 - 负责从 EffectContext 中计算出一组目标
/// </summary>
public abstract class TargetSelector : ScriptableObject
{
    public string selectorName;

    [Tooltip("选择的对象是否变为执行者")]
    public bool chooseExecutor = false;
    [Tooltip("选择的对象是否变为被执行者")]
    public bool chooseExecuted = true;

    public abstract List<ITarget> GetTargets(EffectContext context);

    public virtual void PreviewHighlight(EffectContext context, bool show) { }
}