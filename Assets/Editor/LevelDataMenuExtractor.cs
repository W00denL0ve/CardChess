using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

/// <summary>
/// 关卡数据提取器
/// </summary>
public class LevelDataMenuExtractor : EditorWindow
{
    [MenuItem("Tools/Extract LevelData From Scene")]
    static void ExtractLevelData()
    {
        // ---------- 1. 获取基础地形 Tilemap ----------
        Tilemap baseTilemap = GetTilemapByNames("Base", "Layer0");
        if (baseTilemap == null)
        {
            EditorUtility.DisplayDialog("提取失败", "未找到名为 'Base' 或 'Layer0' 的 Tilemap。", "好");
            return;
        }
        baseTilemap.CompressBounds();

        // ---------- 2. 从基础 Tilemap 生成 LevelGridData ----------
        BoundsInt bounds = baseTilemap.cellBounds;
        int width = bounds.size.x;
        int height = bounds.size.y;
        CellData[] cells = new CellData[width * height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                TerrainTile tile = baseTilemap.GetTile<TerrainTile>(pos);
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

        // 创建 LevelGridData 实例
        LevelGridData gridAsset = ScriptableObject.CreateInstance<LevelGridData>();
        gridAsset.width = width;
        gridAsset.height = height;
        gridAsset.cells = cells;

        // ---------- 3. 解析回合行动 Tilemap ----------
        LevelTurnData turnAsset = ParseRoundTilemaps(baseTilemap);

        // ---------- 4. 创建主 LevelData 资产 ----------
        LevelData mainAsset = ScriptableObject.CreateInstance<LevelData>();
        mainAsset.gridData = gridAsset;
        mainAsset.turnData = turnAsset;

        // ---------- 5. 保存所有资产 ----------
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Substring(2);
        string folderPath = "Assets/ScriptableObjects/LevelData/";
        if (!AssetDatabase.IsValidFolder(folderPath))
            System.IO.Directory.CreateDirectory(Application.dataPath + "/ScriptableObjects/LevelData");
        string gridDataPath = folderPath + "GridData/";
        string turnDataPath = folderPath + "TurnData/";

        // 保存子资产
        gridAsset.name = sceneName + "_Grid";
        string gridPath = gridDataPath + gridAsset.name + ".asset";
        AssetDatabase.CreateAsset(gridAsset, gridPath);

        turnAsset.name = sceneName + "_Turns";
        string turnPath = turnDataPath + turnAsset.name + ".asset";
        AssetDatabase.CreateAsset(turnAsset, turnPath);

        // 保存主资产
        mainAsset.name = sceneName;
        string mainPath = folderPath + mainAsset.name + ".asset";
        
        // 若已存在同名的 LevelData，询问是否覆盖
        if (AssetDatabase.LoadAssetAtPath<LevelData>(mainPath) != null)
        {
            if (!EditorUtility.DisplayDialog("覆盖确认", $"已存在 {mainPath}，是否覆盖？", "覆盖", "取消"))
            {
                // 用户取消，删除已创建的子资产（因为还没被引用）
                AssetDatabase.DeleteAsset(gridPath);
                AssetDatabase.DeleteAsset(turnPath);
                return;
            }
            else
            {
                // 覆盖：删除旧的主资产及其依赖
                AssetDatabase.DeleteAsset(mainPath);
            }
        }

        AssetDatabase.CreateAsset(mainAsset, mainPath);
        // 注册到 Addressables LevelData 组
#if UNITY_EDITOR
        try
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                // 查找或创建 "LevelData" 组
                AddressableAssetGroup group = settings.FindGroup("LevelData");
                if (group == null)
                {
                    group = settings.CreateGroup("LevelData", false, false, false, null, typeof(UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema));
                }

                // 获取资产的 GUID
                string guid = AssetDatabase.AssetPathToGUID(mainPath);
                if (!string.IsNullOrEmpty(guid))
                {
                    // 将资产移入组，并设置地址
                    var entry = settings.CreateOrMoveEntry(guid, group);
                    entry.address = sceneName; // 直接使用场景名作为地址
                }
            }
            else
            {
                Logger.LogWarning("Addressable Settings 未找到，请先初始化 Addressables 系统。");
            }
        }
        catch (System.Exception e)
        {
            Logger.LogWarning($"自动添加 Addressable 失败: {e.Message}");
        }
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("提取成功", 
            $"已生成以下资产：\n" +
            $"主关卡：{mainPath}\n" +
            $"地形网格：{gridPath}\n" +
            $"回合事件：{turnPath}", "确定");
    }

    // ====================================================================
    //  回合行动解析
    // ====================================================================

    /// <summary>
    /// 解析场景中所有名称匹配 "RoundX" 的 Tilemap，生成 LevelTurnData
    /// </summary>
    static LevelTurnData ParseRoundTilemaps(Tilemap baseTilemap)
    {
        BoundsInt baseBounds = baseTilemap.cellBounds;
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();

        // 正则匹配 "Round" 后跟数字的 Tilemap 名称（不区分大小写）
        Regex roundRegex = new Regex(@"^Round(\d+)$", RegexOptions.IgnoreCase);

        // 先收集数据（按回合号排序）
        SortedDictionary<int, List<TurnAction>> roundActions = new SortedDictionary<int, List<TurnAction>>();

        foreach (Tilemap tm in allTilemaps)
        {
            if (tm == baseTilemap) continue;

            Match match = roundRegex.Match(tm.name);
            if (!match.Success) continue;

            int roundNumber = int.Parse(match.Groups[1].Value);
            tm.CompressBounds();

            List<TurnAction> actions = new List<TurnAction>();
            BoundsInt cellBounds = tm.cellBounds;

            foreach (Vector3Int pos in cellBounds.allPositionsWithin)
            {
                TileBase tile = tm.GetTile(pos);
                if (tile == null) continue;

                // 计算逻辑坐标（相对于基础网格原点）
                int col = pos.x - baseBounds.xMin;
                int row = pos.y - baseBounds.yMin;

                // 检查坐标是否在基础网格范围内
                if (col < 0 || col >= baseBounds.size.x || row < 0 || row >= baseBounds.size.y)
                {
                    Logger.LogWarning($"回合 Tilemap '{tm.name}' 的格子 ({pos.x},{pos.y}) 超出基础网格范围，已跳过");
                    continue;
                }

                TurnAction action = CreateActionFromTile(tile, col, row, baseTilemap, pos);
                if (action != null)
                {
                    actions.Add(action);
                }
            }

            if (actions.Count > 0)
            {
                roundActions[roundNumber] = actions;
            }
        }

        // 将收集的数据转为 LevelTurnData
        LevelTurnData turnData = ScriptableObject.CreateInstance<LevelTurnData>();
        turnData.rounds = new List<LevelTurnData.RoundActions>();

        foreach (var kvp in roundActions)
        {
            turnData.rounds.Add(new LevelTurnData.RoundActions
            {
                roundNumber = kvp.Key,
                actions = kvp.Value
            });
        }

        return turnData;
    }

    /// <summary>
    /// 根据 Tile 类型创建对应的 TurnAction
    /// </summary>
    static TurnAction CreateActionFromTile(TileBase tile, int col, int row, Tilemap baseTilemap, Vector3Int tilePos)
    {
        Vector2Int coord = new Vector2Int(col, row);

        if (tile is EnemySpawnTile spawnTile)
        {
            return new EnemySpawnAction
            {
                coord = coord,
                enemyId = spawnTile.enemyId,
                spawnCount = spawnTile.count
            };
        }

        if (tile is CellChangeTile changeTile)
        {
            // 计算最终绝对高度：基础高度 + heightDelta
            int baseHeight = GetBaseHeight(baseTilemap, tilePos);
            int finalHeight = baseHeight + changeTile.heightDelta;

            return new CellChangeAction
            {
                coord = coord,
                newTerrainType = changeTile.targetTerrain,
                newHeight = finalHeight,
                setWalkable = changeTile.setWalkable
            };
        }

        // 未来可在此添加更多 Tile 类型的分支

        Logger.LogWarning($"未处理的 Tile 类型：{tile.GetType().Name}，位置 ({col},{row})");
        return null;
    }

    /// <summary>
    /// 获取基础 Tilemap 上指定位置的高度
    /// </summary>
    static int GetBaseHeight(Tilemap baseTilemap, Vector3Int pos)
    {
        TerrainTile terrainTile = baseTilemap.GetTile<TerrainTile>(pos);
        return terrainTile != null ? terrainTile.height : 0;
    }

    // ====================================================================
    //  辅助方法
    // ====================================================================

    // 辅助方法：按优先级获取 Tilemap
    static Tilemap GetTilemapByNames(params string[] names)
    {
        Tilemap[] all = FindObjectsOfType<Tilemap>();
        foreach (string name in names)
        {
            foreach (var tm in all)
            {
                if (tm.name.ToLower() == name.ToLower())
                    return tm;
            }
        }
        // 若只有一个 Tilemap 直接返回
        if (all.Length == 1)
            return all[0];
        return null;
    }
}