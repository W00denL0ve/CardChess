using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 独立格子移动预览测试 — 自建网格和单位，测试预览栈和视觉反馈
/// 挂载到任意 GameObject 上即可独立运行，不依赖其他测试脚本
/// </summary>
public class TestGridPreview : MonoBehaviour
{
    [Header("单位配置")]
    public UnitConfig playerConfig;
    public GameObject unitPrefab;

    private PreviewManager preview;
    private GridManager gridManager;
    private Unit playerUnit;

    void Start()
    {
        if (!Validate()) return;

        preview = PreviewManager.Instance;
        gridManager = GridManager.Instance;

        SetupGrid();
        playerUnit = SpawnPlayer();

        PrintHelp();
    }

    void OnDestroy()
    {
        ClearUnit();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) ToggleFreeze();
        else if (Input.GetKeyDown(KeyCode.Alpha1)) TestGridMovePreview();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TestEmptyCandidates();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) TestCancel();
    }

    // ==================== 初始化 ====================

    bool Validate()
    {
        if (playerConfig == null || unitPrefab == null)
        {
            Debug.LogError("[TestGridPreview] 请将 playerConfig 和 unitPrefab 拖入 Inspector");
            return false;
        }
        if (PreviewManager.Instance == null)
        {
            Debug.LogError("[TestGridPreview] 场景中需要 PreviewManager");
            return false;
        }
        if (GridManager.Instance == null)
        {
            Debug.LogError("[TestGridPreview] 场景中需要 GridManager");
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
        Debug.Log("[TestGridPreview] 6x6 网格已创建");
    }

    Unit SpawnPlayer()
    {
        Vector2Int pos = new Vector2Int(2, 2);
        GameObject go = Instantiate(unitPrefab, GridManager.Instance.GridToWorld(pos), Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(playerConfig, Faction.Player, pos);
        GridManager.Instance.PlaceUnit(unit, pos);
        if (LevelManager.Instance != null)
            LevelManager.Instance.RegisterUnit(unit);
        Debug.Log($"[TestGridPreview] 玩家单位 {playerConfig.unitId} 生成于 {pos}");
        return unit;
    }

    void ClearUnit()
    {
        if (playerUnit != null)
        {
            LevelManager.Instance?.UnregisterUnit(playerUnit);
            Destroy(playerUnit.gameObject);
        }
    }

    void PrintHelp()
    {
        Debug.Log("[TestGridPreview] 按键: 1-格子移动预览 2-空候选测试 3-取消测试 | F5-编辑器暂停");
    }

    // ==================== 测试方法 ====================

    void ToggleFreeze()
    {
        Debug.Break();
    }

    void TestGridMovePreview()
    {
        if (preview.GetPreviewState() != PreviewState.Idle)
        {
            Debug.LogWarning("[测试1] 预览进行中，请先按3取消");
            return;
        }

        int range = playerUnit.ActionPointLimit;
        var reachable = gridManager.GetReachableCells(playerUnit.GridPosition, range);
        Debug.Log($"[测试1] 格子移动预览: 行动力{range}, 可选{reachable.Count}格");

        preview.EnterGridPreview(playerUnit, reachable,
            (pos) =>
            {
                Debug.Log($"[测试1] 确认移动至 {pos}");
                preview.PopCurrentStep();
            },
            () => Debug.Log("[测试1] 取消（第一步不应触发）")
        );
    }

    void TestEmptyCandidates()
    {
        if (preview.GetPreviewState() != PreviewState.Idle)
        {
            Debug.LogWarning("[测试2] 预览进行中，请先取消");
            return;
        }

        Debug.Log("[测试2] 传入空候选列表 → 预期触发 onCancel");
        preview.EnterGridPreview(playerUnit, new List<Vector2Int>(),
            _ => Debug.LogError("[测试2] 不应触发确认!"),
            () => Debug.Log("[测试2] ✓ 空候选触发 onCancel（正确）")
        );
    }

    void TestCancel()
    {
        if (preview.GetPreviewState() == PreviewState.Idle)
        {
            Debug.Log("[测试3] 无活跃预览");
            return;
        }

        Debug.Log("[测试3] ESC/右键取消将通过预览管理器自动处理。也可在场景中右键/Esc测试。");
    }
}
