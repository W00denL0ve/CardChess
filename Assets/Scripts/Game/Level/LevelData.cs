using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "CardChess/Levels/LevelData")]
public class LevelData : ScriptableObject
{
    // 存储格子数据
    public LevelGridData gridData;
    // 存储回合数据
    public LevelTurnData turnData;
    // 玩家出生点位置列表（从 PlayerSpawnTile 提取）
    public List<Vector2Int> playerSpawnPositions = new();
    // 目标点位置列表（从 GoalTile 提取），供 ReachGoalCondition 使用
    public List<Vector2Int> goalPositions = new();
    // 胜利条件根节点（[SerializeReference] 支持多态，由提取器自动生成或手动配置）
    [SerializeReference]
    public VictoryCondition rootCondition;
}