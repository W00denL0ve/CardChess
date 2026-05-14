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

        // ---------- 3. 处理回合事件层（TODO） ----------
        // 当前仅创建空的 LevelTurnData，后续会解析 RoundN Tilemap 填充
        LevelTurnData turnAsset = ScriptableObject.CreateInstance<LevelTurnData>();
        // TODO: 遍历名称匹配 "Round*" 的 Tilemap，解析事件并填充 turnAsset

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
        string turnDatarPath = folderPath + "TurnData/";

        // 保存子资产
        gridAsset.name = sceneName + "_Grid";
        string gridPath = gridDataPath + gridAsset.name + ".asset";
        AssetDatabase.CreateAsset(gridAsset, gridPath);

        turnAsset.name = sceneName + "_Turns";
        string turnPath = turnDatarPath + turnAsset.name + ".asset";
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
                // 覆盖：删除旧的主资产及其依赖（需手动处理子资产引用？简单起见直接删除旧主资产）
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
                Debug.LogWarning("Addressable Settings 未找到，请先初始化 Addressables 系统。");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"自动添加 Addressable 失败: {e.Message}");
        }
#endif
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("提取成功", 
            $"已生成以下资产：\n" +
            $"主关卡：{mainPath}\n" +
            $"地形网格：{gridPath}\n" +
            $"回合事件：{turnPath}（待填充）", "确定");
    }

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