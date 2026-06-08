/// <summary>
/// 阶段变化事件,包含旧阶段和新阶段
/// </summary>
public class TurnPhaseChangedEvent : GameEvent
{
    public int turnNumber;
    public TurnPhase oldPhase;
    public TurnPhase newPhase;

    public TurnPhaseChangedEvent(int turnNumber, TurnPhase oldPhase, TurnPhase newPhase)
    {
        this.turnNumber = turnNumber;
        this.oldPhase = oldPhase;
        this.newPhase = newPhase;
    }
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

/// <summary>
/// 回合结束事件
/// </summary>
public class TurnEndedEvent : GameEvent
{
    public int turnNumber;

    public TurnEndedEvent(int turnNumber)
    {
        this.turnNumber = turnNumber;
    }
}