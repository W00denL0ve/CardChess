using System.Linq;

/// <summary>
/// 保护指定单位 — 该单位必须存活
/// </summary>
[System.Serializable]
public class ProtectUnitCondition : VictoryCondition
{
    public string targetUnitId;

    public override bool IsMet() =>
        LevelManager.Instance?.AllUnits.Any(u => u.UnitId == targetUnitId && u.IsAlive) == true;

    /// <summary>目标单位死亡 → 不可达成</summary>
    public override bool IsImpossible() =>
        LevelManager.Instance?.AllUnits.Any(u => u.UnitId == targetUnitId && u.IsAlive) == false;
}
