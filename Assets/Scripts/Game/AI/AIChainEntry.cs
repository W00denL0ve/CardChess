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
    public AITargetType targetType = AITargetType.Hostile;

    [Tooltip("链的类型，影响策略权重")]
    public ChainCategory category = ChainCategory.Attack;

    [Tooltip("基础分")]
    public int baseScore = 10;
}

/// <summary>
/// AI 目标类型
/// </summary>
public enum AITargetType
{
    [Tooltip("对敌方(不含中立单位)使用")]
    Hostile,
    [Tooltip("对敌方(包括中立单位)使用")]
    Hostile_Neutral,
    [Tooltip("对盟友(不包括自身)使用")]
    Ally,
    [Tooltip("对自身使用")]
    Self,
    [Tooltip("对自身或盟友使用")]
    Ally_Self,
    [Tooltip("对任意存活单位使用")]
    Any,
    [Tooltip("对格子使用")]
    Grid
}

/// <summary>
/// AI 链类型，用于匹配策略权重
/// </summary>
public enum ChainCategory
{
    Attack,
    Heal,
    Buff,
    Debuff
}
