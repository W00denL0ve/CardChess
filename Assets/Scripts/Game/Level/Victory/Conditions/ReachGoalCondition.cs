using System.Linq;

/// <summary>
/// 到达任意目标点 — 引用 LevelData.goalPositions
/// </summary>
[System.Serializable]
public class ReachGoalCondition : VictoryCondition
{
    public override bool IsMet()
    {
        var level = LevelManager.Instance?.CurrentLevel;
        if (level == null || level.goalPositions.Count == 0) return false;

        return LevelManager.Instance.AllUnits
            .Any(u => u.Faction == Faction.Player && u.IsAlive
                      && level.goalPositions.Contains(u.GridPosition));
    }

    public override bool IsImpossible() =>
        LevelManager.Instance?.AllUnits.Any(u => u.Faction == Faction.Player && u.IsAlive) == false;
}
