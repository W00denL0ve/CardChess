using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 玩家出生点 Tile — 标记地图上玩家单位的放置位置
/// 不持有任何单位数据，只标记位置；由 LevelManager 在加载时根据存档阵容随机部署
/// </summary>
[CreateAssetMenu(fileName = "PlayerSpawnTile", menuName = "Tiles/PlayerSpawnTile")]
public class PlayerSpawnTile : TileBase
{
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }
}
