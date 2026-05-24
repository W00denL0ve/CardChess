using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害效果 - 对 executor 自身造成伤害
/// 异步时序：OnExecute 计算 → PlayAnimation 播放攻击/受击 → OnComplete 扣血
/// </summary>
[CreateAssetMenu(fileName = "SelfDamageEffect", menuName = "CardChess/EffectChain/Effects/SelfDamage")]
public class SelfDamageEffect : Effect
{
    [Header("倍率依据")]
    public Multiplier multiplier;

    private float damage;

    private Unit executor;

    private float GetValueByMultiplier(UnitBaseValue b, Multiplier m)
    {
        return b.attack * m.attack + b.intelligence * m.intelligence + b.currentHealth * m.currentHealth
            + b.maxHealth * m.maxHealth + (b.maxHealth - b.currentHealth) * m.lossHealth + b.hasMoved * m.hasMoved
            + b.physicalDefense * m.physicalDefense + b.magicDefense * m.magicDefense;
    }

    public override void OnExecute(EffectContext context)
    {
        executor = context.GetExecutorUnit();
        // 根据倍率获取施方基础值
        damage = GetValueByMultiplier(executor.baseValue, multiplier);
    }

    public override void OnComplete(EffectContext context)
    {
        // 直接扣血，并显示特殊跳字
        executor.TakeDamage((int)damage);
        FloatingNumberManager.Instance.ShowSpecialDamage(executor.GridPosition, (int)damage);
    }
}