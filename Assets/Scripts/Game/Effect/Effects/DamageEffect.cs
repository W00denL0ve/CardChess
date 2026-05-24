using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Multiplier
{
    [Tooltip("力量")]
    public float attack;

    [Tooltip("智慧")]
    public float intelligence;

    [Tooltip("当前生命")]
    public float currentHealth;

    [Tooltip("已损失生命")]
    public float lossHealth;

    [Tooltip("最大生命")]
    public float maxHealth;

    [Tooltip("本回合移动步数")]
    public float hasMoved;

    [Tooltip("物理防御")]
    public float physicalDefense;

    [Tooltip("魔法防御")]
    public float magicDefense;
}

/// <summary>
/// 伤害效果 - 对 context.executed 单位造成伤害
/// 异步时序：OnExecute 计算 → PlayAnimation 播放攻击/受击 → OnComplete 扣血
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "CardChess/EffectChain/Effects/Damage")]
public class DamageEffect : Effect, IAnimatedEffect
{
    [Header("倍率依据")]
    public Multiplier multiplier;

    [Header("卡牌修饰器")]
    public List<Modifier> modifiers = new();

    public DamageType damageType = DamageType.Physical;

    private int _finalDamage;

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

        List<Modifier> modifiers_or = new();
        List<Modifier> modifiers_ed = new();
        float defenseBase = 0;

        // 根据倍率获取施方基础值
        float damageBase = GetValueByMultiplier(executor.baseValue, multiplier);

        if (damageType == DamageType.Physical)
        {
            // 获取施方所有物理伤害修饰器
            modifiers_or = executor.modifierManager.GetModifiers(ModifierField.Physic);
            // 获取受方所有物理防御修饰器
            modifiers_ed = executed.modifierManager.GetModifiers(ModifierField.PhysicalDefense);

            // 获取受方物理防御基础值
            defenseBase = executed.baseValue.physicalDefense;
        }
        else if (damageType == DamageType.Magical)
        {
            // 获取施方所有魔法伤害修饰器
            modifiers_or = executor.modifierManager.GetModifiers(ModifierField.Magic);
            // 获取受方所有魔法防御修饰器
            modifiers_ed = executor.modifierManager.GetModifiers(ModifierField.MagicDefense);

            // 获取受方魔法防御基础值
            defenseBase = executed.baseValue.magicDefense;
        }
        // 加入卡牌修饰器
        modifiers_or.AddRange(modifiers);
        // 注入公式
        _finalDamage = AttributeCulculator.CulculateFinalValue(damageBase, modifiers_or, defenseBase, modifiers_ed);
    }

    public IEnumerator PlayAnimation(EffectContext context)
    {
        Unit executor = context.GetExecutorUnit();
        Unit executed = context.GetExecutedUnit();
        if (executed == null || executor == null) yield break;

        var executorApp = executor.Appearance;

        if (executorApp != null)
        {
            executorApp.FaceTo(executed.GridPosition);
            executorApp.SetAnimationFrameAction(() => ExecuteOnAnimationFrame(executor, executed, context));
            yield return executorApp.PlayAttack(damageType);
        }
    }

    public void ExecuteOnAnimationFrame(Unit executor, Unit executed, EffectContext context)
    {
        executed.TakeDamage(_finalDamage, context); // 扣血
        var executedApp = executed.Appearance;
        if (executedApp != null)
        {
            executedApp.FaceTo(executor.GridPosition); // 更改朝向
            executedApp.StartCoroutine(executedApp.PlayHitReaction()); // 播放受击动画
        }
        AudioManager.Instance.PlaySound(damageType == DamageType.Physical ? "hitPhysical" : "hitMagic"); // 播放伤害音效

        FloatingNumberType FLType = damageType == DamageType.Physical ? FloatingNumberType.Physical : FloatingNumberType.Magical;
        FloatingNumberManager.Instance.ShowNumber(executed.GridPosition, _finalDamage, FLType); // 显示浮字
    }

    public override void OnComplete(EffectContext context)
    {
        // 无需后处理
    }
}