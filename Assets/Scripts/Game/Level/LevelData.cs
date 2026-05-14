using UnityEngine;
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    // 存储格子数据
    public LevelGridData gridData;
    // 存储回合数据
    public LevelTurnData turnData;
    // todo: 未来可加：public UnitSpawnConfig unitSpawnConfig;
}