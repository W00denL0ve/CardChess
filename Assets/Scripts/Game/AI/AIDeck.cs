using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI 行为配置 — 敌人的效果链预设
/// </summary>
[CreateAssetMenu(menuName = "CardChess/AI/AIDeck")]
public class AIDeck : ScriptableObject
{
    [Header("每回合能量")]
    [Tooltip("该单位每回合可用的行动能量")]
    public int energyPerTurn = 3;

    [Header("AI 策略")]
    [Tooltip("该单位的决策风格")]
    public AIStrategy strategy = AIStrategy.Balanced;

    [Header("效果链")]
    [Tooltip("AI可的可选项")]
    [SerializeField]
    public List<AIChainEntry> entries = new();
}
