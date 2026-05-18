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
    public GameObject unitPrefab;

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
            Debug.LogError("[TestAsync] 场景中需要 AsyncEffectExecutor");
        SubscribeEvents();
        BuildProgrammaticCard();
        PrintHelp();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
        CleanupUnits();
    }

    void Update()
    {
        if (executor == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (importedCard != null)
                ExecuteCard(importedCard);
            else
                Debug.LogWarning("[TestAsync] 请将 CardData 资产拖入 importedCard");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1)) TestDamageStep();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TestMoveStep();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) TestFullCard();
        else if (Input.GetKeyDown(KeyCode.Alpha4)) TestAsyncDelay();
        else if (Input.GetKeyDown(KeyCode.F5)) Debug.Break();
    }

    // ====================================================================
    //  初始化
    // ====================================================================

    bool Validate()
    {
        if (warriorConfig == null || mageConfig == null || unitPrefab == null)
        {
            Debug.LogError("[TestAsync] 请将 warriorConfig、mageConfig、unitPrefab 拖入 Inspector");
            return false;
        }
        if (GridManager.Instance == null || LevelManager.Instance == null || PreviewManager.Instance == null)
        {
            Debug.LogError("[TestAsync] 场景中需要 GridManager、LevelManager、PreviewManager");
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
        Debug.Log("[TestAsync] 6x6 网格已创建");
    }

    Unit SpawnUnit(UnitConfig config, Faction faction, Vector2Int pos)
    {
        UnitFactory.SetUnitPrefab(unitPrefab);
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

    void SubscribeEvents()
    {
        GameEventChannel.Register<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Register<UnitMovedEvent>(OnMoved);
    }

    void UnsubscribeEvents()
    {
        GameEventChannel.Unregister<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Unregister<UnitMovedEvent>(OnMoved);
    }

    void PrintHelp()
    {
        Debug.Log("[TestAsync] 按键: 0-拖入卡牌 1-伤害 2-移动 3-双步骤 4-异步延迟测试");
    }

    // ====================================================================
    //  构建程序化卡牌
    // ====================================================================

    void BuildProgrammaticCard()
    {
        programmaticCard = ScriptableObject.CreateInstance<CardData>();
        programmaticCard.cardName = "Programmatic Card";
        programmaticCard.description = "效果链: 选目标→伤害→选格子→移动";
        programmaticCard.chains = new List<EffectChain>
        {
            new EffectChain
            {
                steps = new List<ChainStep>
                {
                    new SelectorStep { selector = CreateUnitSelector() },
                    new EffectStep { effect = CreateDamageEffect() },
                    new SelectorStep { selector = CreateCellSelector() },
                    new EffectStep { effect = CreateMoveEffect() }
                }
            }
        };
        Debug.Log("[TestAsync] 程序化卡牌已构建 (2个步骤)");
    }

    UnitSelector CreateUnitSelector()
    {
        var sel = ScriptableObject.CreateInstance<UnitSelector>();
        sel.factions = (UnitSelector.FactionMask)(1 << (int)Faction.Enemy);
        return sel;
    }

    CellPathSelector CreateCellSelector()
    {
        var sel = ScriptableObject.CreateInstance<CellPathSelector>();
        sel.range = warrior.MovePoints;
        sel.includeOrigin = true;
        return sel;
    }

    DamageEffect CreateDamageEffect()
    {
        var eff = ScriptableObject.CreateInstance<DamageEffect>();
        eff.effectName = "造成伤害";
        eff.damageAmount = 15;
        eff.damageType = DamageType.Physical;
        return eff;
    }

    MoveEffect CreateMoveEffect()
    {
        var eff = ScriptableObject.CreateInstance<MoveEffect>();
        eff.effectName = "移动至目标格";
        eff.requirePath = true;
        return eff;
    }

    // ====================================================================
    //  测试方法
    // ====================================================================

    void ExecuteCard(CardData card)
    {
        Logger.Log($"[执行] 开始执行卡牌: {card.name} ({card.chains?.Count ?? 0} 条效果链)");
        executor.ExecuteCardChainsAsync(card, () =>
        {
            Logger.Log($"[执行] 卡牌效果全部完成！");
        });
    }

    void TestDamageStep()
    {
        var step = new SelectorStep { selector = CreateUnitSelector() };
        var step2 = new EffectStep { effect = CreateDamageEffect() };
        var ctx = new EffectContext
        {
            sourceCard = programmaticCard,
            executor = new UnitTarget(warrior),
            executed = new UnitTarget(warrior)
        };
        Debug.Log("[测试1] 请选择敌方单位");
        executor.ExecuteStepAsync(step, ctx, null);
        executor.ExecuteStepAsync(step2, ctx, () =>
        {
            Debug.Log($"[测试1] 完成！法师 HP: {mage.CurrentHealth}/{mage.MaxHealth}");
        });
    }

    void TestMoveStep()
    {
        var step = new SelectorStep { selector = CreateCellSelector() };
        var step2 = new EffectStep { effect = CreateMoveEffect() };
        var ctx = new EffectContext
        {
            sourceCard = programmaticCard,
            executor = new UnitTarget(warrior),
            executed = new UnitTarget(warrior)
        };
        Debug.Log("[测试2] 请选择移动目标格");
        executor.ExecuteStepAsync(step, ctx, null);
        executor.ExecuteStepAsync(step2, ctx, () =>
        {
            Debug.Log($"[测试2] 完成！战士位置: ({warrior.GridPosition.x},{warrior.GridPosition.y})");
        });
    }

    void TestAsyncDelay()
    {
        var animEffect = ScriptableObject.CreateInstance<TestAnimatedDelayEffect>();
        animEffect.delay = 1.0f;

        var step = new EffectStep { effect = animEffect };
        var ctx = new EffectContext
        {
            sourceCard = programmaticCard,
            executor = new UnitTarget(warrior),
            executed = new UnitTarget(warrior)
        };

        Debug.Log("[测试4] 异步延迟效果: OnExecute → 等待 1s → OnComplete");
        executor.ExecuteStepAsync(step, ctx, () =>
        {
            Debug.Log("[测试4] ✅ 步骤完成！1秒延迟确认");
        });
    }

    void TestFullCard()
    {
        Logger.Log("[测试3] 完整双步骤卡牌: 选目标伤害 → 选格子移动");
        executor.ExecuteCardChainsAsync(programmaticCard, () =>
        {
            Logger.Log($"[测试3] 完成！法师 HP: {mage.CurrentHealth}/{mage.MaxHealth}");
            Logger.Log($"[测试3] 战士位置: ({warrior.GridPosition.x},{warrior.GridPosition.y})");
        });
    }

    void OnHealthChanged(UnitHealthChangedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} HP: {evt.OldHealth} → {evt.NewHealth}");
    }

    void OnMoved(UnitMovedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} 移动 {evt.From} → {evt.To}");
    }
}
