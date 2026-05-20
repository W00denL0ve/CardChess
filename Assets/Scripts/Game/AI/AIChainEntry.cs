using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI 效果链条目 — 一条效果链及其评分参数
/// </summary>
[Serializable]
public class AIChainEntry
{
    [Tooltip("要执行的效果链")]
    [SerializeField]
    public EffectChain chain;

    [Tooltip("执行此链消耗的能量")]
    public int energyCost = 1;

    [Tooltip("使用后冷却回合数")]
    public int cooldown;
    [Tooltip("整场战斗最多用几次（0=不限）")]
    public int maxUsePerBattle;

    [Tooltip("此链的目标类型")]
    public AITargetType targetType = AITargetType.Enemy;

    [Tooltip("链的类型，影响策略权重")]
    public ChainCategory category = ChainCategory.Attack;

    [Tooltip("基础分")]
    public int baseScore = 10;
}

/// <summary>
/// AI 目标偏好类型
/// </summary>
public enum AITargetType
{
    [Tooltip("攻击敌方单位")]
    Enemy,
    [Tooltip("治疗/增益己方单位")]
    Ally,
    [Tooltip("对自身使用")]
    Self,
    [Tooltip("任意存活单位")]
    Any
}

/// <summary>
/// AI 链类型，用于匹配策略权重
/// </summary>
public enum ChainCategory
{
    Attack,
    Heal,
    Buff,
    Debuff,
    Utility
}
