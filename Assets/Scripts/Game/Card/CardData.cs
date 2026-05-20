using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 效果链 — 可序列化的步骤列表包装
/// </summary>
[Serializable]
public class EffectChain
{
    [SerializeReference]
    public List<ChainStep> steps = new();
}

[CreateAssetMenu(fileName = "CardData", menuName = "CardChess/Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite artwork;
    public string description;

    [Header("打出后去向")]
    public DestinationOnPlay destination = DestinationOnPlay.Discard;

    [Header("消耗")]
    public int Cost = 1;

    [Header("回合结束保留")]
    public bool retain = false;

    [Header("卡牌颜色")]
    public CardColorPreset colorPreset;
    public Color cardColor = Color.white;

    [Tooltip("多条效果链，每条链是一个顺序执行的步骤序列")]
    public List<EffectChain> chains = new();
}

public enum CardColorPreset
{
    None,
    Red,
    Green,
    Blue
}

public enum DestinationOnPlay
{
    Discard,     // 弃牌堆
    Destroy,     // 销毁堆（本局移除）
    ReturnToHand // 返回手牌
}
