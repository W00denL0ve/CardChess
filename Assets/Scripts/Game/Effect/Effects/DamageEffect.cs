using System.Collections;
using UnityEngine;

/// <summary>
/// 伤害效果 - 对 context.executed 单位造成伤害
/// 异步时序：OnExecute 计算 → PlayAnimation 播放攻击/受击 → OnComplete 扣血
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Game/Effect/Damage")]
public class DamageEffect : Effect, IAnimatedEffect
{
    public int damageAmount;
    public DamageType damageType = DamageType.Physical;

    private int _finalDamage;

    public override void OnExecute(EffectContext context)
    {
        Unit target = context.GetExecutedUnit();
        if (target == null) return;
        int defense = target.GetDefenseFor(damageType);
        _finalDamage = Mathf.Max(1, damageAmount - defense);
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        Unit attacker = context.GetExecutorUnit();
        Unit target = context.GetExecutedUnit();
        if (target == null || attacker == null) yield break;

        var atkApp = attacker.GetComponent<UnitAppearance>();
        var tgtApp = target.GetComponent<UnitAppearance>();

        if (atkApp != null)
        {
            // 注册击打回调 → 击打帧同时触发受击动画 + 扣血
            atkApp.RegisterHitFrameTarget(target, () => target.TakeDamage(_finalDamage, context));
            yield return atkApp.PlayAttack();
        }

        // 等受击动画播完（若已在攻击播放期间播完，立即返回）
        if (tgtApp != null)
            yield return tgtApp.WaitForAnimation("Hit");
    }

    public override void OnComplete(EffectContext context)
    {
        // 伤害已在击打帧时扣除
    }
}