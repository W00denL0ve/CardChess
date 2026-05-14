using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 格子变化 Tile - 在 Tilemap 上标记需要修改的格子属性
/// </summary>
[CreateAssetMenu(fileName = "CellChangeTile", menuName = "Tiles/CellChangeTile")]
public class CellChangeTile : TileBase
{
    /// <summary>目标地形类型</summary>
    public TerrainType targetTerrain;

    /// <summary>高度变化量（相对于基础高度）</summary>
    public int heightDelta;

    /// <summary>是否设置可行走（null 表示不修改）</summary>
    public bool? setWalkable;

    /// <summary>预览精灵</summary>
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }
}
