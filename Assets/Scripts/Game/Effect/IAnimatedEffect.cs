using System.Collections;

/// <summary>
/// 动画效果接口 — 实现了此接口的 Effect 可在 OnExecute 后播放一段视觉动画，
/// 动画完成后自动调用 OnComplete
/// </summary>
public interface IAnimatedEffect
{
    /// <summary>返回一个协程，在 OnExecute 之后、OnComplete 之前执行</summary>
    IEnumerator PlayAnimation(EffectContext context);
}
