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
[CreateAssetMenu(fileName = "HealEffect", menuName = "CardChess/EffectChain/Effects/Heal")]
public class HealEffect : Effect, IAnimatedEffect
{
    [Header("倍率依据")]
    public Multiplier multiplier;

    [Header("卡牌修饰器")]
    public List<Modifier> modifiers = new();

    private int _finalHeal;

    private float GetValueByMultiplier(UnitBaseValue b, Multiplier m)
    {
        return b.attack * m.attack + b.intelligence * m.intelligence + b.currentHealth * m.currentHealth
            + b.maxHealth * m.maxHealth + (b.maxHealth - b.currentHealth) * m.lossHealth + b.hasMoved * m.hasMoved
            + b.physicalDefense * m.physicalDefense + b.magicDefense * m.magicDefense;
    }

    public override void OnExecute(EffectContext context)
    {
        Unit executed = context.GetExecutedUnit();
        Unit executor = context.GetExecutorUnit();
        if (executed == null) return;

        List<Modifier> modifiers_or;

        // 根据倍率获取施方基础值
        float healBase = GetValueByMultiplier(executor.baseValue, multiplier);
        // 获取施方所有治疗修饰器
        modifiers_or = executor.modifierManager.GetModifiers(ModifierField.Heal);

        // 增加卡牌修饰器
        modifiers_or.AddRange(modifiers);

        _finalHeal = AttributeCulculator.CulculateFinalValue(healBase, modifiers_or);
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        Unit executor = context.GetExecutorUnit();
        Unit executed = context.GetExecutedUnit();
        if (executed == null || executor == null) yield break;

        // 更新施法者朝向
        executor.UpdateFacingDirection(executed.GridPosition - executor.GridPosition);

        var atkApp = executor.Appearance;
        if (atkApp != null)
        {
            atkApp.SetAnimationFrameAction(() =>ExecuteOnAnimationFrame(executor, executed, context));
            yield return atkApp.PlayCast();
        }
    }


    public void ExecuteOnAnimationFrame(Unit executor, Unit executed, EffectContext context)
    {
        executed.Heal(_finalHeal, context); // 治疗
        var executedApp = executed.Appearance;
        if (executedApp != null)
        {
            executedApp.StartCoroutine(executedApp.PlayHeal()); // 播放受治疗动画
        }
        AudioManager.Instance.PlaySound(AudioName.healSound); // 播放治疗音效
        FloatingNumberManager.Instance.ShowNumber(executed.GridPosition, _finalHeal, FloatingNumberType.Healing); // 显示浮动数字
    }

    public override void OnComplete(EffectContext context)
    {
        // 伤害已在击打帧时扣除
    }
}