using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tile资产创建器
/// </summary>
[CreateAssetMenu(fileName = "TerrainTile", menuName = "Tiles/TerrainTile")]
public class TerrainTile : TileBase
{
    public TerrainType terrainType = TerrainType.unreachable;
    public int height = 0;
    public Sprite previewSprite;  // 预览用精灵（可留空）

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite; // 如果有则显示，否则保持 null
    }
}