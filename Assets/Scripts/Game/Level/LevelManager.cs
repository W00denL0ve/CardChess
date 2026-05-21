using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private TurnManager turnManager;
    private GridManager gridManager;

    private List<Unit> allUnits = new();
    public IReadOnlyList<Unit> AllUnits => allUnits;

    private Unit lastDeadUnit;

    /// <summary>当前关卡数据引用</summary>
    public LevelData CurrentLevel { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GameEventChannel.Register<UnitDeathEvent>(HandleUnitDeath);
    }

    void OnDisable()
    {
        GameEventChannel.Unregister<UnitDeathEvent>(HandleUnitDeath);
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
            allUnits.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        allUnits.Remove(unit);
    }

    /// <summary>
    /// 获取unit所有对立阵营单位，参数控制是否包括中立阵营（中立阵营默认与其他都对立）
    /// </summary>
    public List<Unit> GetEnemiesOf(Unit unit, bool includeNeutral)
    {
        List<Unit> enemies = allUnits.Where(u => u.Faction != unit.Faction && u.IsAlive).ToList();
        if (!includeNeutral) enemies.RemoveAll(u => u.Faction == Faction.Neutral);
        return enemies;
    }

    /// <summary>
    /// 返回unit同阵营单位，参数控制是否包含自身
    /// </summary>
    public List<Unit> GetAlliesOf(Unit unit, bool includeSelf)
    {
        List<Unit> allies = allUnits.Where(u => u.Faction == unit.Faction && u.IsAlive).ToList();
        if (!includeSelf) allies.Remove(unit);
        return allies;
    }

    /// <summary>
    /// 获取所有指定阵营Unit
    /// </summary>
    public List<Unit> GetUnitsByFaction(Faction faction)
    {
        return allUnits.Where(u => u.Faction == faction && u.IsAlive).ToList();
    }

    private void HandleUnitDeath(UnitDeathEvent evt)
    {
        UnregisterUnit(evt.Unit);
        lastDeadUnit = evt.Unit;
    }

    /// <summary>
    /// 获取上一个阵亡的Unit
    /// </summary>
    public Unit GetLastDeadUnit()
    {
        return lastDeadUnit;
    }

    /// <summary>
    /// 加载关卡
    /// </summary>
    public void Initialize(LevelData levelData, List<CardData> initialCards = null)
    {
        CurrentLevel = levelData;

        turnManager = FindObjectOfType<TurnManager>();
        gridManager = FindObjectOfType<GridManager>();

        gridManager.LoadGridData(levelData.gridData);
        Logger.Log("棋盘加载完成");

        turnManager.LoadTurnData(levelData.turnData);
        Logger.Log("回合信息加载完成");

        // 部署玩家单位（在所有敌方/中立单位生成前执行）
        SpawnPlayerUnits(levelData.playerSpawnPositions);

        // 初始化牌库
        var deckManager = FindObjectOfType<DeckManager>();
        if (deckManager != null)
            deckManager.Initialize(initialCards);
        else
            Logger.LogWarning("[LevelManager] 场景中未找到 DeckManager");

        // 初始化胜利条件检查器
        var checker = FindObjectOfType<VictoryChecker>();
        if (checker != null)
            checker.Initialize(levelData.rootCondition);
        else
            Logger.LogWarning("[LevelManager] 场景中未找到 VictoryChecker，跳过胜利条件初始化");
    }

    /// <summary>
    /// 根据存档阵容随机部署玩家单位到出生点
    /// </summary>
    private void SpawnPlayerUnits(List<Vector2Int> spawnPositions)
    {
        List<UnitConfig> roster = SaveManager.Instance?.GetPlayerRoster();
        if (roster == null || roster.Count == 0)
        {
            Logger.LogWarning("[LevelManager] 无玩家阵容存档，跳过玩家单位部署");
            return;
        }

        if (spawnPositions == null || spawnPositions.Count == 0)
        {
            Logger.LogWarning("[LevelManager] 地图上无玩家出生点，跳过部署");
            return;
        }

        // 打乱位置和阵容，实现随机配对
        Shuffle(spawnPositions);
        Shuffle(roster);

        int count = Mathf.Min(spawnPositions.Count, roster.Count);
        for (int i = 0; i < count; i++)
        {
            Unit unit = UnitFactory.Spawn(roster[i], spawnPositions[i], Faction.Player);
            if (unit != null)
                Logger.Log($"[LevelManager] 部署 {roster[i].unitId} → ({spawnPositions[i].x},{spawnPositions[i].y})");
        }

        if (roster.Count > spawnPositions.Count)
            Logger.LogWarning($"[LevelManager] 玩家有 {roster.Count} 个角色，但只有 {spawnPositions.Count} 个出生点");
    }

    /// <summary>Fisher-Yates 洗牌</summary>
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}