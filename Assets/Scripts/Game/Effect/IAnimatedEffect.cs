using System.Collections;

/// <summary>
/// 动画效果接口 — 实现了此接口的 Effect 可在 OnExecute 后播放一段视觉动画，
/// 动画完成后自动调用 OnComplete
/// </summary>
public interface IAnimatedEffect
{
    /// <summary>返回一个协程，在 OnExecute 之后、OnComplete 之前执行</summary>
    IEnumerator PlayAnimation(EffectContext context);

    /// <summary>
    /// 在动画表现对齐的帧进行的方法
    /// </summary>
    /// <param name="executor"></param>
    /// <param name="executed"></param>
    /// <param name="context"></param>
    void ExecuteOnAnimationFrame(Unit executor, Unit executed, EffectContext context);
}
