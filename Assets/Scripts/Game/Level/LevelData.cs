using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    // 存储格子数据
    public LevelGridData gridData;
    // 存储回合数据
    public LevelTurnData turnData;
    // 玩家出生点位置列表（从 PlayerSpawnTile 提取）
    public List<Vector2Int> playerSpawnPositions = new();
}