/// <summary>
/// Buff 施加事件
/// </summary>
public class BuffAppliedEvent : GameEvent
{
    public Unit Unit { get; }
    public BuffInstance Instance { get; }

    public BuffAppliedEvent(Unit unit, BuffInstance instance)
    {
        Unit = unit;
        Instance = instance;
    }
}

/// <summary>
/// Buff 移除事件
/// </summary>
public class BuffRemovedEvent : GameEvent
{
    public Unit Unit { get; }
    public Buff BuffData { get; }

    public BuffRemovedEvent(Unit unit, Buff buffData)
    {
        Unit = unit;
        BuffData = buffData;
    }
}
