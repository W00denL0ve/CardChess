using System.Collections;
using UnityEngine;

/// <summary>
/// 测试用动画效果 — 在 OnExecute 后等待 delay 秒再 OnComplete
/// </summary>
[CreateAssetMenu(menuName = "CardChess/EffectChain/Effects/Test/AnimatedDelay")]
public class TestAnimatedDelayEffect : Effect, IAnimatedEffect
{
    [Tooltip("动画等待秒数")]
    public float delay = 0.5f;

    public override void OnExecute(EffectContext context)
    {
        Debug.Log($"[TestAnimated] OnExecute: 延迟 {delay}s 后完成");
    }

    public override void OnComplete(EffectContext context)
    {
        Debug.Log("[TestAnimated] OnComplete: 效果结束");
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        Debug.Log($"[TestAnimated] PlayAnimation: 开始等待 {delay}s");
        yield return new WaitForSeconds(delay);
        Debug.Log($"[TestAnimated] PlayAnimation: 等待结束");
    }
}
