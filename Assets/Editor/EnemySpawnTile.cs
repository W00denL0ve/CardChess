using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 敌人生成 Tile - 在 Tilemap 上标记敌人出生点
/// </summary>
[CreateAssetMenu(fileName = "EnemySpawnTile", menuName = "Tiles/EnemySpawnTile")]
public class EnemySpawnTile : TileBase
{
    /// <summary>敌人 ID（用于从配置表或 Addressables 加载）</summary>
    public string enemyId;

    /// <summary>生成数量</summary>
    public int count = 1;

    /// <summary>预览精灵</summary>
    public Sprite previewSprite;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = previewSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (count < 1) count = 1;
    }
#endif
}
