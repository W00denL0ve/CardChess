using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 异步效果系统测试 — 创建网格+单位，测试 EffectStep 链式执行
/// 按键: 0-执行拖入卡牌 1-选择目标+伤害 2-选格子移动 3-程序化完整卡牌
/// </summary>
public class TestAsyncEffect : MonoBehaviour
{
    [Header("单位配置")]
    public UnitConfig warriorConfig;
    public UnitConfig mageConfig;

    [Header("可选：拖入已有卡牌资产")]
    [SerializeField] private CardData importedCard;

    private Unit warrior;
    private Unit mage;
    private CardData programmaticCard;
    private AsyncEffectExecutor executor;

    void Start()
    {
        if (!Validate()) return;
        SetupGrid();
        warrior = SpawnUnit(warriorConfig, Faction.Player, new Vector2Int(1, 1));
        mage = SpawnUnit(mageConfig, Faction.Enemy, new Vector2Int(3, 3));
        executor = FindObjectOfType<AsyncEffectExecutor>();
        if (executor == null)
            Logger.LogError("[TestAsync] 场景中需要 AsyncEffectExecutor");
        GameEventChannel.Dispatch(new LevelEnteredEvent("test"));
    }

    void OnDestroy()
    {
        CleanupUnits();
    }

    void Update()
    {
        if (executor == null) return;

    }

    // ====================================================================
    //  初始化
    // ====================================================================

    bool Validate()
    {
        if (warriorConfig == null || mageConfig == null)
        {
            Logger.LogError("[TestAsync] 请将 warriorConfig、mageConfig 拖入 Inspector");
            return false;
        }
        if (GridManager.Instance == null || LevelManager.Instance == null || PreviewManager.Instance == null)
        {
            Logger.LogError("[TestAsync] 场景中需要 GridManager、LevelManager、PreviewManager");
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
        Logger.Log("[TestAsync] 6x6 网格已创建");
    }

    Unit SpawnUnit(UnitConfig config, Faction faction, Vector2Int pos)
    {
        return UnitFactory.Spawn(config, pos, faction);
    }

    void CleanupUnits()
    {
        foreach (var u in new[] { warrior, mage })
        {
            if (u != null)
            {
                LevelManager.Instance?.UnregisterUnit(u);
                Destroy(u.gameObject);
            }
        }
    }
}