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

    public List<Unit> GetEnemiesOf(Unit unit)
    {
        return allUnits.Where(u => u.Faction != unit.Faction && u.IsAlive).ToList();
    }

    public List<Unit> GetAlliesOf(Unit unit)
    {
        return allUnits.Where(u => u.Faction == unit.Faction && u != unit && u.IsAlive).ToList();
    }

    public List<Unit> GetUnitsOf(Faction faction)
    {
        return allUnits.Where(u => u.Faction == faction && u.IsAlive).ToList();
    }

    private void HandleUnitDeath(UnitDeathEvent evt)
    {
        UnregisterUnit(evt.Unit);
    }

    /// <summary>
    /// 加载关卡的具体实现，由GameManager调用
    /// </summary>
    public void Initialize(LevelData levelData)
    {
        turnManager = FindObjectOfType<TurnManager>();
        gridManager = FindObjectOfType<GridManager>();

        gridManager.LoadGridData(levelData.gridData);
        Logger.Log("棋盘加载完成");

        turnManager.LoadTurnData(levelData.turnData);
        Logger.Log("回合信息加载完成");
    }


}