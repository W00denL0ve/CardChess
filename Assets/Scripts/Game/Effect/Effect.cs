using UnityEngine;

/// <summary>
/// 效果基类 — 纯数据 + 执行逻辑
/// 自身不包含目标选择器，选择器已分离到 EffectStep 中
///
/// 时序约定：
///   OnExecute → 数据在表现上对齐"打击瞬间"（例如攻击动画打出去的一下）
///   OnComplete → 数据效果完全结束，对齐表现上结束的时刻
/// </summary>
public abstract class Effect : ScriptableObject
{
    [Header("效果基本信息")]
    public string effectName;
    
    public Sprite icon;

    /// <summary>
    /// 先发数据执行的时刻
    /// Buff 添加、移动请求等实际数据变更在此发生
    /// 如果需要对齐动画表现，需要继承IAnimatedEffect接口
    /// </summary>
    public virtual void OnExecute(EffectContext context) { }

    /// <summary>
    /// 效果完全结束的时刻，进行后处理
    /// </summary>
    public virtual void OnComplete(EffectContext context) { }
}