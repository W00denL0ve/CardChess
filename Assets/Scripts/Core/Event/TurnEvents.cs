/// <summary>
/// 阶段变化事件（包括了开始和结束）
/// </summary>
public class PhaseChangedEvent : GameEvent
{
    public int turnNumber;
    public TurnPhase oldPhase;
    public TurnPhase newPhase;
}

/// <summary>
/// 回合开始事件
/// </summary>
public class TurnStartedEvent : GameEvent
{
    public int turnNumber;

    public TurnStartedEvent(int turnNumber)
    {
        this.turnNumber = turnNumber;
    }
}