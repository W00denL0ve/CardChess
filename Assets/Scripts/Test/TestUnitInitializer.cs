using System.Collections.Generic;
using UnityEngine;

public class TestUnitInitializer : MonoBehaviour
{
    public UnitConfig warriorConfig;
    public UnitConfig mageConfig;
    public GameObject unitPrefab; // 带有 Unit 和 UnitVisual 的预制体

    private Unit warrior;
    private Unit mage;
    private List<Unit> spawnedUnits = new();

    void Start()
    {
        // 1. 初始化网格
        SetupGrid();

        // 2. 生成单位
        warrior = SpawnUnit(warriorConfig, Faction.Player, new Vector2Int(1, 1));
        mage = SpawnUnit(mageConfig, Faction.Enemy, new Vector2Int(3, 3));

        // 3. 注册到 LevelManager
        LevelManager.Instance.RegisterUnit(warrior);
        LevelManager.Instance.RegisterUnit(mage);

        // 4. 测试事件监听（示例）
        GameEventChannel.Register<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Register<UnitDeathEvent>(OnUnitDeath);
        GameEventChannel.Register<UnitMovedEvent>(OnUnitMoved);

        Debug.Log("Test units ready. Press keys: 1-伤害, 2-治疗, 3-移动, 4-加Buff, 5-毒Buff, 6-击杀, 7-阵营查询");
    }

    void SetupGrid()
    {
        // 创建一个 5x5 的简单网格，所有格子可行走
        var gridData = ScriptableObject.CreateInstance<LevelGridData>();
        gridData.width = 5;
        gridData.height = 5;
        gridData.cells = new CellData[25];
        for (int i = 0; i < 25; i++)
        {
            gridData.cells[i] = new CellData { terrainType = TerrainType.ground, height = 0 };
        }
        GridManager.Instance.LoadGridData(gridData);
    }

    Unit SpawnUnit(UnitConfig config, Faction faction, Vector2Int pos)
    {
        GameObject go = Instantiate(unitPrefab, GridManager.Instance.GridToWorld(pos), Quaternion.identity);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(config, faction, pos);
        GridManager.Instance.PlaceUnit(unit, pos);
        spawnedUnits.Add(unit);
        return unit;
    }

    void Update()
    {
        // 测试控制
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestDamage();
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestHeal();
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestMove();
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestBuff();
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestPoison();
        if (Input.GetKeyDown(KeyCode.Alpha6)) TestKill();
        if (Input.GetKeyDown(KeyCode.Alpha7)) TestQuery();
    }

    void TestDamage()
    {
        // 战士对法师造成物理伤害
        EffectContext ctx = new EffectContext { caster = warrior.gameObject };
        int raw = warrior.Attack + warrior.DamageBonus; // 模拟基础伤害
        int final = Mathf.Max(0, raw - mage.GetDefenseFor(DamageType.Physical));
        mage.TakeDamage(final, ctx);
    }

    void TestHeal()
    {
        mage.Heal(20);
    }

    void TestMove()
    {
        // 测试移动战士到 (2,2)
        warrior.RequestMove(new Vector2Int(2, 2));
    }

    void TestBuff()
    {
        // 应用攻击力提升 Buff
        var buff = ScriptableObject.CreateInstance<TestAttackUpBuff>(); // 实际应引用资产
        warrior.BuffContainer.ApplyBuff(buff);
    }

    void TestPoison()
    {
        var poison = ScriptableObject.CreateInstance<TestPoisonBuff>();
        mage.BuffContainer.ApplyBuff(poison);
    }

    void TestKill()
    {
        // 直接造成 9999 伤害
        mage.TakeDamage(9999);
    }

    void TestQuery()
    {
        var enemies = LevelManager.Instance.GetEnemiesOf(warrior);
        Debug.Log($"Warrior enemies: {enemies.Count}");
        var allies = LevelManager.Instance.GetAlliesOf(warrior);
        Debug.Log($"Warrior allies: {allies.Count}");
    }

    // 事件回调
    void OnHealthChanged(UnitHealthChangedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} HP: {evt.OldHealth} -> {evt.NewHealth}");
    }

    void OnUnitDeath(UnitDeathEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} died at {evt.DeathPosition}");
    }

    void OnUnitMoved(UnitMovedEvent evt)
    {
        Debug.Log($"[Event] {evt.Unit.UnitId} moved {evt.From} -> {evt.To}");
    }
}