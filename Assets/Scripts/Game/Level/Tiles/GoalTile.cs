using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 目标点 Tile — 标记地图上需要到达的位置
/// 由提取器扫描并写入 LevelData.goalPositions
/// </summary>
[CreateAssetMenu(fileName = "GoalTile", menuName = "Tiles/GoalTile")]
public class GoalTile : TileBase
{
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }
}
