using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI 效果链条目 — 一条效果链及其使用限制
/// </summary>
[Serializable]
public class AIChainEntry
{
    public EffectChain chain;

    [Header("决策参数")]
    public int priority;            // 优先级（越大越优先选）

    [Header("使用限制")]
    public int cooldown;            // 使用后冷却回合数
    public int minRange;            // 最小距离（曼哈顿）
    public int maxRange;            // 最大距离（曼哈顿, 0=不限）
    public float hpThreshold;       // 自身血量低于此比例时优先（0~1, 0=禁用）
    public int maxUsePerBattle;     // 整场战斗最多用几次（0=不限）
}
