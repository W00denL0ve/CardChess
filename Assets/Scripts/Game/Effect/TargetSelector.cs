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
    /// 为 true 时触发 executor←old executed, executed←target 的上下文链式更新。
    /// 为 false 时仅更新 executed←target，executor 保持不变。
    /// 第一类选择器（直接对场景中目标生效）设为 false。
    /// </summary>
    public bool ChangesContext { get; protected set; } = true;

    /// <summary>
    /// 核心方法：根据上下文返回目标列表
    /// </summary>
    public abstract List<ITarget> GetTargets(EffectContext context);

    /// <summary>
    /// 效果执行前的高亮调用 — 子类可覆写以在场景中标记被选中的目标
    /// </summary>
    /// <param name="context">效果上下文</param>
    /// <param name="show">true=高亮，false=清除高亮</param>
    public virtual void PreviewHighlight(EffectContext context, bool show) { }
}