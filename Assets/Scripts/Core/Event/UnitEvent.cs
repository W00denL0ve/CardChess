using UnityEngine;

// 生命值变化事件
public class UnitHealthChangedEvent : GameEvent
{
    public Unit Unit { get; }
    public int OldHealth { get; }
    public int NewHealth { get; }
    public int MaxHealth { get; }
    public UnitHealthChangedEvent(Unit unit, int oldH, int newH, int maxH) { Unit = unit; OldHealth = oldH; NewHealth = newH; MaxHealth = maxH; }
}

// 死亡事件
public class UnitDeathEvent : GameEvent
{
    public Unit Unit { get; }
    public Vector2Int DeathPosition { get; }
    public EffectContext KillContext { get; }
    public UnitDeathEvent(Unit unit, EffectContext killContext = null) { Unit = unit; DeathPosition = unit.GridPosition; KillContext = killContext; }
}

public class UnitAcquireMovePointEvent : GameEvent
{
    public Unit Unit { get; }
    public int Points { get; }

    public UnitAcquireMovePointEvent(Unit unit, int points) {Unit = unit; Points = points;}
}

// 移动请求事件
public class UnitMoveRequestEvent : GameEvent
{
    public Unit Unit { get; }
    public Vector2Int From { get; }
    public Vector2Int To { get; }
    public EffectContext Context { get; }
    public UnitMoveRequestEvent(Unit unit, Vector2Int from, Vector2Int to, EffectContext context = null) { Unit = unit; From = from; To = to; Context = context; }
}

// 移动完成事件
public class UnitMovedEvent : GameEvent
{
    public Unit Unit { get; }
    public Vector2Int From { get; }
    public Vector2Int To { get; }
    public EffectContext Context { get; }
    public UnitMovedEvent(Unit unit, Vector2Int from, Vector2Int to, EffectContext context = null) { Unit = unit; From = from; To = to; Context = context; }
}
