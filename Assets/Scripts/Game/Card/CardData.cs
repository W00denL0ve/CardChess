using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 效果链 — 可序列化的步骤列表包装
/// </summary>
[Serializable]
public class EffectChain
{
    public List<GameEffectStep> steps = new();
}

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite artwork;
    public string description;

    [Tooltip("多条效果链，每条链是一个顺序执行的步骤序列")]
    public List<EffectChain> chains = new();
}
