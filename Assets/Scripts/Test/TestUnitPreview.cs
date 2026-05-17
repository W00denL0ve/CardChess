using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 独立单位选择预览测试 — 自建网格和多个单位，测试单位选择、嵌套预览
/// 挂载到任意 GameObject 上即可独立运行
/// </summary>
public class TestUnitPreview : MonoBehaviour
{
    [Header("单位配置")]
    public UnitConfig playerConfig;
    public UnitConfig enemyConfig;
    public GameObject unitPrefab;

    private PreviewManager preview;
    private GridManager gridManager;
    private readonly List<Unit> spawnedUnits = new();

    void Start()
    {
        if (!Validate()) return;

        preview = PreviewManager.Instance;
        gridManager = GridManager.Instance;

        SetupGrid();
        SpawnAllUnits();
        PrintHelp();
    }

    void OnDestroy()
    {
        CleanupAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) ToggleFreeze();
        else if (Input.GetKeyDown(KeyCode.Alpha1)) TestUnitSelect();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TestNestedPreview();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) TestQuickCleanup();
    }

    // ==================== 初始化 ====================

    bool Validate()
    {
        if (playerConfig == null || enemyConfig == null || unitPrefab == null)
        {
            Debug.LogError("[TestUnitPreview] 请将配置和预制体拖入 Inspector");
            return false;
        }
        if (PreviewManager.Instance == null)
        {
            Debug.LogError("[TestUnitPreview] 场景中需要 PreviewManager");
            return false;
        }
        if (GridManager.Instance == null)
        {
            Debug.LogError("[TestUnitPreview] 场景中需要 GridManager");
            return false;
        }
        return true;
    }

    void SetupGrid()
    {
        var gridData = ScriptableObject.CreateInstance<LevelGridData>();
        gridData.width = 6;
        gridData.height = 6;
        gridData.cells = new CellData[36];
        for (int i = 0; i < 36; i++)
            gridData.cells[i] = new CellData { terrainType = TerrainType.ground, height = 0 };

        GridManager.Instance.LoadGridData(gridData);
        Debug.Log("[TestUnitPreview] 6x6 网格已创建");
    }

    void SpawnAllUnits()
    {
        // 玩家单位在 (1,1)
        SpawnUnit(playerConfig, Faction.Player, new Vector2Int(1, 1));
        // 敌方单位在 (3,3) 和 (4,1)
        SpawnUnit(enemyConfig, Faction.Enemy, new Vector2Int(3, 3));
        SpawnUnit(enemyConfig, Faction.Enemy, new Vector2Int(4, 1));
    }

    void SpawnUnit(UnitConfig config, Faction faction, Vector2Int pos)
    {
        GameObject go = Instantiate(unitPrefab, GridManager.Instance.GridToWorld(pos), Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(config, faction, pos);
        GridManager.Instance.PlaceUnit(unit, pos);
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterUnit(unit);
        spawnedUnits.Add(unit);
        Debug.Log($"[TestUnitPreview] 生成 {config.unitId}({faction}) 于 {pos}");
    }

    void CleanupAll()
    {
        foreach (var u in spawnedUnits)
        {
            if (u != null)
            {
                LevelManager.Instance?.UnregisterUnit(u);
                Destroy(u.gameObject);
            }
        }
        spawnedUnits.Clear();
    }

    void PrintHelp()
    {
        Debug.Log("[TestUnitPreview] 按键: 1-单位选择预览 2-嵌套预览 3-清理 | F5-编辑器暂停");
    }

    // ==================== 测试方法 ====================

    void ToggleFreeze()
    {
        Debug.Break();
    }

    void TestUnitSelect()
    {
        if (preview.GetPreviewState() != PreviewState.Idle)
        {
            Debug.LogWarning("[测试1] 预览进行中，请先按3清理");
            return;
        }

        // 获取敌方单位作为候选
        var player = spawnedUnits.Find(u => u.Faction == Faction.Player);
        if (player == null) return;

        var enemies = LevelManager.Instance.GetEnemiesOf(player);
        Debug.Log($"[测试1] 单位选择预览: 候选 {enemies.Count} 个单位");

        preview.EnterUnitPreview(enemies,
            (unit) =>
            {
                Debug.Log($"[测试1] ✓ 确认选择 {unit.UnitId}");
                preview.PopCurrentStep();
            },
            () => Debug.Log("[测试1] 取消")
        );
    }

    void TestNestedPreview()
    {
        if (preview.GetPreviewState() != PreviewState.Idle)
        {
            Debug.LogWarning("[测试2] 预览进行中，请先按3清理");
            return;
        }

        var player = spawnedUnits.Find(u => u.Faction == Faction.Player);
        if (player == null) return;

        var enemies = LevelManager.Instance.GetEnemiesOf(player);
        if (enemies.Count == 0)
        {
            Debug.LogWarning("[测试2] 没有敌方单位");
            return;
        }

        Debug.Log("[测试2] 嵌套预览: 第一步选敌方单位 → 第二步选格子");

        preview.EnterUnitPreview(enemies,
            (selectedEnemy) =>
            {
                Debug.Log($"[测试2] 第一步: 选中 {selectedEnemy.UnitId}，进入第二步");

                int range = selectedEnemy.ActionPointLimit;
                var reachable = gridManager.GetReachableCells(selectedEnemy.GridPosition, range);
                preview.EnterGridPreview(selectedEnemy, reachable,
                    (cell) =>
                    {
                        Debug.Log($"[测试2] 第二步: ✓ 移动至 {cell}，嵌套完成");
                        preview.PopCurrentStep(); // 弹出 GridMove
                        preview.PopCurrentStep(); // 弹出 UnitSelect → 完成
                    },
                    () => Debug.Log("[测试2] 第二步取消，回退到第一步")
                );
            },
            () => Debug.Log("[测试2] 第一步取消（不应触发）")
        );
    }

    void TestQuickCleanup()
    {
        if (preview.GetPreviewState() == PreviewState.Idle)
        {
            Debug.Log("[测试3] 无活跃预览，无需清理");
            return;
        }

        // 强制模拟 ESC 事件
        GameEventChannel.Dispatch(new EscapePressedEvent());
        Debug.Log("[测试3] 已发送 EscapePressedEvent 强制退出");
    }
}
