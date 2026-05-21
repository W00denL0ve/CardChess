using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 胜利条件 Tile 基类 — 放在 WinCondition Tilemap 层，用于在场景中可视化配置胜利条件
/// </summary>
public abstract class ConditionTile : TileBase
{
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }
}
