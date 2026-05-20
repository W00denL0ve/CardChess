using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害效果 - 对 context.executed 单位造成伤害
/// 异步时序：OnExecute 计算 → PlayAnimation 播放攻击/受击 → OnComplete 扣血
///
/// 伤害公式（基准值 = 攻击者基础攻击力，再合并所有修饰器）：
///   addSum = 攻击者.Attack + 卡牌.addDamage + Unit.DamageBonus.Add
///   mulSum = 卡牌.multiplyDamage × Unit.DamageBonus.Multiply
///   finalAddSum = 卡牌.finalAddDamage + Unit.DamageBonus.FinalAdd
///   finalMulSum = 卡牌.finalMultiplyDamage × Unit.DamageBonus.FinalMultiply
///   原始伤害 = (addSum × mulSum + finalAddSum) × finalMulSum
///   最终伤害 = max(1, round(原始伤害) - 目标基础防御)
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "CardChess/EffectChain/Effects/Damage")]
public class DamageEffect : Effect, IAnimatedEffect
{
    [Header("卡牌自身修饰器（仅本次伤害有效）")]
    public float addDamage;
    public float multiplyDamage = 1f;
    public float finalAddDamage;
    public float finalMultiplyDamage = 1f;

    public DamageType damageType = DamageType.Physical;

    private int _finalDamage;

    public override void OnExecute(EffectContext context)
    {
        Unit target = context.GetExecutedUnit();
        Unit attacker = context.GetExecutorUnit();
        if (target == null) return;

        // 基准值 = 攻击者基础攻击力
        float addSum = attacker?.Attack ?? 0;
        float mulSum = 1f;
        float finalAddSum = 0f;
        float finalMulSum = 1f;

        // 卡牌自身修饰器
        addSum      += addDamage;
        mulSum      *= multiplyDamage;
        finalAddSum += finalAddDamage;
        finalMulSum *= finalMultiplyDamage;

        // 攻击者 DamageBonus 的持久修饰器（buff/装备）
        var attr = attacker?.AttributeManager?.GetAttribute(AttributeType.DamageBonus);
        if (attr != null)
        {
            foreach (var mod in attr.modifiers)
            {
                switch (mod.type)
                {
                    case ModifierType.Add:            addSum      += mod.value; break;
                    case ModifierType.Multiply:       mulSum      *= mod.value; break;
                    case ModifierType.FinalAdd:       finalAddSum += mod.value; break;
                    case ModifierType.FinalMultiply:  finalMulSum *= mod.value; break;
                }
            }
        }

        float damage = (addSum * mulSum + finalAddSum) * finalMulSum;

        // 减去目标基础防御
        int defense = target.GetDefenseFor(damageType);
        _finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage) - defense);
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
        // 受击动画已由 OnHitFrame 自动触发，无需额外等待
    }

    public override void OnComplete(EffectContext context)
    {
        // 伤害已在击打帧时扣除
    }
}