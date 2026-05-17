using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// 独立战斗系统测试 — 创建网格 + 单位，测试伤害/治疗/移动/Buff
/// 挂载到任意 GameObject 上即可独立运行
/// </summary>
public class TestGridCombat : MonoBehaviour
{
    [Header("单位配置")]
    public UnitConfig warriorConfig;
    public UnitConfig mageConfig;
    public GameObject unitPrefab;

    [Header("测试用 Buff 资产（需拖入）")]
    public Buff attackUpBuff;
    public Buff poisonBuff;

    private Unit warrior;
    private Unit mage;
    private readonly List<Unit> spawnedUnits = new();

    void Start()
    {
        if (!ValidateConfig()) return;

        SetupGrid();
        warrior = SpawnUnit(warriorConfig, Faction.Player, new Vector2Int(1, 1));
        mage = SpawnUnit(mageConfig, Faction.Enemy, new Vector2Int(3, 3));

        SubscribeEvents();
        PrintHelp();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
        CleanupUnits();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) ToggleFreeze();
        else if (Input.GetKeyDown(KeyCode.Alpha1)) TestDamage();
        else if (Input.GetKeyDown(KeyCode.Alpha2)) TestHeal();
        else if (Input.GetKeyDown(KeyCode.Alpha3)) TestMove();
        else if (Input.GetKeyDown(KeyCode.Alpha4)) TestBuffAttack();
        else if (Input.GetKeyDown(KeyCode.Alpha5)) TestBuffPoison();
        else if (Input.GetKeyDown(KeyCode.Alpha6)) TestKill();
        else if (Input.GetKeyDown(KeyCode.Alpha7)) TestQuery();
    }

    // ==================== 初始化 ====================

    bool ValidateConfig()
    {
        if (warriorConfig == null || mageConfig == null || unitPrefab == null)
        {
            Debug.LogError("[TestGridCombat] 请将 warriorConfig、mageConfig、unitPrefab 拖入 Inspector");
            return false;
        }
        if (GridManager.Instance == null)
        {
            Debug.LogError("[TestGridCombat] 场景中需要 GridManager");
            return false;
        }
        if (LevelManager.Instance == null)
        {
            Debug.LogError("[TestGridCombat] 场景中需要 LevelManager");
            return false;
        }
        return true;
    }

    void SetupGrid()
    {
        var gridData = ScriptableObject.CreateInstance<LevelGridData>();
        gridData.width = 5;
        gridData.height = 5;
        gridData.cells = new CellData[25];
        for (int i = 0; i < 25; i++)
            gridData.cells[i] = new CellData { terrainType = TerrainType.ground, height = 0 };

        GridManager.Instance.LoadGridData(gridData);
        Debug.Log("[TestGridCombat] 5x5 网格已创建");
    }

    Unit SpawnUnit(UnitConfig config, Faction faction, Vector2Int pos)
    {
        GameObject go = Instantiate(unitPrefab, GridManager.Instance.GridToWorld(pos), Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(config, faction, pos);
        GridManager.Instance.PlaceUnit(unit, pos);
        LevelManager.Instance.RegisterUnit(unit);
        spawnedUnits.Add(unit);
        Debug.Log($"[TestGridCombat] 生成 {config.unitId} 于 {pos}");
        return unit;
    }

    void CleanupUnits()
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

    void SubscribeEvents()
    {
        GameEventChannel.Register<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Register<UnitDeathEvent>(OnDeath);
        GameEventChannel.Register<UnitMovedEvent>(OnMoved);
    }

    void UnsubscribeEvents()
    {
        GameEventChannel.Unregister<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Unregister<UnitDeathEvent>(OnDeath);
        GameEventChannel.Unregister<UnitMovedEvent>(OnMoved);
    }

    void PrintHelp()
    {
        Debug.Log("[TestGridCombat] 按键: 1-伤害 2-治疗 3-移动 4-攻击Buff 5-毒Buff 6-击杀 7-阵营查询 | F5-编辑器暂停");
    }

    // ==================== 测试方法 ====================

    void ToggleFreeze()
    {
        Debug.Break();
    }

    void TestDamage()
    {
        if (warrior == null || mage == null) return;
        int raw = warrior.Attack + warrior.DamageBonus;
        int final = Mathf.Max(1, raw - mage.GetDefenseFor(DamageType.Physical));
        var ctx = new EffectContext { executor = new UnitTarget(warrior), executed = new UnitTarget(warrior) };
        mage.TakeDamage(final, ctx);
        Debug.Log($"[测试1] 战士→法师 物理伤害: 原始{raw}, 最终{final}, 法师HP={mage.CurrentHealth}/{mage.MaxHealth}");
    }

    void TestHeal()
    {
        if (mage == null) return;
        mage.Heal(20);
        Debug.Log($"[测试2] 法师治疗+20, HP={mage.CurrentHealth}/{mage.MaxHealth}");
    }

    void TestMove()
    {
        if (warrior == null) return;
        Vector2Int target = new Vector2Int(2, 2);
        Cell cell = GridManager.Instance.GetCell(target.x, target.y);
        if (cell == null || !cell.isWalkable || cell.OccupyingUnit != null)
        {
            Debug.LogWarning("[测试3] (2,2) 不可移动");
            return;
        }
        warrior.RequestMove(target);
        Debug.Log($"[测试3] 战士请求移动至 {target}");
    }

    void TestBuffAttack()
    {
        if (warrior == null || attackUpBuff == null)
        {
            Debug.LogWarning("[测试4] 请将 attackUpBuff 拖入 Inspector");
            return;
        }
        int before = warrior.Attack;
        warrior.BuffContainer.ApplyBuff(attackUpBuff);
        Debug.Log($"[测试4] 战士攻击力 Buff: {before} → {warrior.Attack}");
    }

    void TestBuffPoison()
    {
        if (mage == null || poisonBuff == null)
        {
            Debug.LogWarning("[测试5] 请将 poisonBuff 拖入 Inspector");
            return;
        }
        mage.BuffContainer.ApplyBuff(poisonBuff);
        Debug.Log($"[测试5] 法师已中毒");
        // 手动触发一次 OnTurnEnd 以立即看到效果
        mage.BuffContainer.OnTurnEnd();
    }

    void TestKill()
    {
        if (mage == null) return;
        mage.TakeDamage(9999);
        Debug.Log($"[测试6] 法师被击杀, isAlive={mage.IsAlive}");
    }

    void TestQuery()
    {
        if (warrior == null) return;
        var enemies = LevelManager.Instance.GetEnemiesOf(warrior);
        var allies = LevelManager.Instance.GetAlliesOf(warrior);
        Debug.Log($"[测试7] 战士的敌人:{enemies.Count}, 友方:{allies.Count}");
        foreach (var e in enemies) Debug.Log($"  - 敌方: {e.UnitId} HP={e.CurrentHealth}");
        foreach (var a in allies) Debug.Log($"  - 友方: {a.UnitId} HP={a.CurrentHealth}");
    }

    // ==================== 事件回调 ====================

    void OnHealthChanged(UnitHealthChangedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} HP: {evt.OldHealth} → {evt.NewHealth}/{evt.MaxHealth}");
    }

    void OnDeath(UnitDeathEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} 死亡于 {evt.DeathPosition}");
    }

    void OnMoved(UnitMovedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} 移动 {evt.From} → {evt.To}");
    }
}
