using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 单位生成 Tile - 在 Tilemap 上标记单位出生点
/// </summary>
[CreateAssetMenu(fileName = "UnitSpawnTile", menuName = "Tiles/UnitSpawnTile")]
public class UnitSpawnTile : TileBase
{
    /// <summary>随机生成池 — 按权重 + 难度抽一个单位</summary>
    public SpawnGroup spawnGroup;

    /// <summary>兜底单位配置 — spawnGroup 为 null 时使用</summary>
    public UnitConfig fallbackUnitConfig;

    /// <summary>允许生成的地形类型（为空则表示不限制）</summary>
    public List<TerrainType> allowedTerrains = new List<TerrainType> { TerrainType.ground };

    /// <summary>出生点被占用时，在该范围内搜索最近的可用格子（0=不搜索）</summary>
    public int searchRange = 0;

    /// <summary>预览精灵</summary>
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }
}
