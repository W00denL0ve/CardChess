using System.Linq;

/// <summary>
/// 全歼敌人 — 场上无存活 Enemy 阵营单位
/// 设计师如需等待所有增援到齐，配合 SurviveRoundsCondition AND 组合即可
/// </summary>
[System.Serializable]
public class KillAllEnemiesCondition : VictoryCondition
{
    public override bool IsMet() =>
        LevelManager.Instance?.AllUnits.Any(u => u.Faction == Faction.Enemy && u.IsAlive) == false;

    public override bool IsImpossible() =>
        LevelManager.Instance?.AllUnits.Any(u => u.Faction == Faction.Player && u.IsAlive) == false;
}
