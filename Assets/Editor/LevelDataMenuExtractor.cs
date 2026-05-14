using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// 关卡数据提取器
/// </summary>
public class LevelDataMenuExtractor : EditorWindow
{
    [MenuItem("Tools/Extract LevelData From Scene")]
    static void ExtractLevelData()
    {
        List<Tilemap> layerTilemaps = GetLayerTilemaps();
        if (layerTilemaps.Count == 0)
        {
            EditorUtility.DisplayDialog("提取失败", "未找到任何包含'Layer'的Tilemap。\n请确保至少有一个Tilemap名称包含'Layer'。", "好");
            return;
        }

        // 创建LevelData
        LevelGridData levelData = ScriptableObject.CreateInstance<LevelGridData>();
        foreach (Tilemap tilemap in layerTilemaps)
        {
            tilemap.CompressBounds(); // 立即缩小边界
            BoundsInt bounds = tilemap.cellBounds;
            int width = bounds.size.x;
            int height = bounds.size.y;
            CellData[] cells = new CellData[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int pos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                    TerrainTile tile = tilemap.GetTile<TerrainTile>(pos);
                    if (tile != null)
                    {
                        cells[y * width + x] = new CellData
                        {
                            terrainType = tile.terrainType,
                            height = tile.height
                        };
                    }
                    else
                    {
                        cells[y * width + x] = new CellData
                        {
                            terrainType = TerrainType.unreachable,
                            height = 0
                        };
                    }
                }
            }
            levelData.AddLayer(width, height, cells);
        }

        // 保存资产
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string defaultName = sceneName + "_LevelData";
        if (!AssetDatabase.IsValidFolder("Assets/Levels"))
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Levels");
        
        string path = EditorUtility.SaveFilePanelInProject("保存 LevelData", defaultName, "asset", "选择保存路径", "Assets/Levels");
        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(levelData);
            return;
        }

        // 设置资产名称（与文件名一致）
        string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
        levelData.name = assetName;
        
        // 检查是否已存在，如果存在则覆盖
        LevelGridData existing = AssetDatabase.LoadAssetAtPath<LevelGridData>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(levelData, existing);
            DestroyImmediate(levelData);
            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(levelData, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("提取成功", $"已将 {layerTilemaps.Count} 层数据保存到：\n{path}", "确定");
    }

    static List<Tilemap> GetLayerTilemaps()
    {
        List<Tilemap> result = new List<Tilemap>();
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        foreach (var tm in allTilemaps)
        {
            if (tm.name.ToLower().Contains("layer"))
                result.Add(tm);
        }
        // 如果没有包含Layer的，取第一个
        if (result.Count == 0 && allTilemaps.Length > 0)
            result.Add(allTilemaps[0]);
        return result;
    }
}