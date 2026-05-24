using System.Linq;
using UnityEngine;

/// <summary>
/// 存活指定回合数 — 计数器在 EndPhase 递增
/// </summary>
[System.Serializable]
public class SurviveRoundsCondition : VictoryCondition
{
    public int requiredRounds = 5;
    public int currentRounds { get; private set; }

    public override void Initialize()
    {
        currentRounds = 0;
        GameEventChannel.Register<TurnPhaseChangedEvent>(OnPhaseChanged);
    }

    public override void Cleanup()
    {
        GameEventChannel.Unregister<TurnPhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnPhaseChanged(TurnPhaseChangedEvent evt)
    {
        if (evt.newPhase == TurnPhase.End)
            currentRounds++;
    }

    public override bool IsMet() => currentRounds >= requiredRounds;

    public override bool IsImpossible() =>
        LevelManager.Instance?.AllUnits.Any(u => u.Faction == Faction.Player && u.IsAlive) == false;
}
