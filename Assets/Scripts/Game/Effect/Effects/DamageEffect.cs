using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
/// 攻击位置, 定义在DamageEffect文件下，Unit提供查询方式，Effect负责计算伤害
/// </summary>
public enum AttackPosition
{
    Front,
    Side,
    Back
}

/// <summary>
/// 公共工具类，提供去重获取修饰器的方法
/// </summary>
public static class ModifierHelper
{
    /// <summary>
    /// 去重获取修饰器
    /// </summary>
    /// <param name="currentModifiers">当前修饰器</param>
    /// <param name="newModifiers">新修饰器</param>
    public static void GetUniqueModifiers(ref List<Modifier> currentModifiers, List<Modifier> newModifiers)
    {
        foreach (var m in newModifiers)
        {
            if (!currentModifiers.Contains(m))
                currentModifiers.Add(m);
        }
    }
}


/// <summary>
/// 伤害效果 - 对 context.executed 单位造成伤害
/// 异步时序：OnExecute 计算 → PlayAnimation 播放攻击/受击 → OnComplete 扣血
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "CardChess/EffectChain/Effects/Damage")]
public class DamageEffect : Effect, IAnimatedEffect
{
    [Header("基础倍率")]
    public Multiplier multiplier;

    [Header("修饰器")]
    public List<Modifier> modifiers = new();

    [Header("伤害类型")]
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
            // 获取卡牌所有物理伤害修饰器
            modifiers_or.AddRange(modifiers.Where(m => m.field == ModifierField.Physic));

            // 获取受方所有物理防御修饰器
            modifiers_ed = executed.modifierManager.GetModifiers(ModifierField.PhysicalDefense);

            // 获取受方物理防御基础值
            defenseBase = executed.baseValue.physicalDefense;
        }
        else if (damageType == DamageType.Magical)
        {
            // 获取施方所有魔法伤害修饰器
            modifiers_or = executor.modifierManager.GetModifiers(ModifierField.Magic);
            // 获取卡牌所有魔法伤害修饰器
            modifiers_or.AddRange(modifiers.Where(m => m.field == ModifierField.Magic));

            // 获取受方所有魔法防御修饰器
            modifiers_ed = executor.modifierManager.GetModifiers(ModifierField.MagicDefense);

            // 获取受方魔法防御基础值
            defenseBase = executed.baseValue.magicDefense;
        }

        // 从施方获取攻击方位
        AttackPosition attackPosition = executor.GetAttackPositionFromTarget(executed);

        // 若为背刺，获取所有背刺伤害修饰器
        if (attackPosition == AttackPosition.Back)
        {
            // 获取施方所有背刺伤害修饰器
            var modifiers_back = executor.modifierManager.GetModifiers(ModifierField.BackAttack);
            // 去重增加
            ModifierHelper.GetUniqueModifiers(ref modifiers_or, modifiers_back);
            // 获取卡牌所有背刺伤害修饰器
            modifiers_or.AddRange(modifiers.Where(m => m.field.HasFlag(ModifierField.BackAttack)));
        }
        
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
            executorApp.AppearanceFaceTo(executed.GridPosition);
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
            executedApp.AppearanceFaceTo(executor.GridPosition); // 更改朝向
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