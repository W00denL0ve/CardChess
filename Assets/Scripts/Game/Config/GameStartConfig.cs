using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 开局配置 — 拖选初始角色和卡牌，GameManager 在 StartNewGame 时读取
/// 临时替代"主城选择界面"
/// </summary>
[CreateAssetMenu(fileName = "GameStartConfig", menuName = "CardChess/GameStartConfig")]
public class GameStartConfig : ScriptableObject
{
    [Header("初始角色")]
    [Tooltip("UnitConfig 的 Addressable 地址列表")]
    public List<string> initialRoster = new() { "Warrior" };

    [Header("初始卡牌")]
    [Tooltip("卡牌的 CardData 引用，开局加入牌库")]
    public List<CardData> initialCards = new();
}
