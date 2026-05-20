using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI 行为配置 — 敌人的效果链预设
/// </summary>
[CreateAssetMenu(menuName = "CardChess/AI/AIDeck")]
public class AIDeck : ScriptableObject
{
    public List<AIChainEntry> entries = new();
}
