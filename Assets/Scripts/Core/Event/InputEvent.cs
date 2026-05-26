using UnityEngine;

/// <summary>
/// 左键单击格子事件
/// </summary>
public class CellLeftClickedEvent : GameEvent
{
    public Vector2Int GridPosition { get; }
    public CellLeftClickedEvent(Vector2Int pos) => GridPosition = pos;
}

/// <summary>
/// 左键双击格子事件
/// </summary>
public class CellDoubleClickedEvent : GameEvent
{
    public Vector2Int GridPosition { get; }
    public CellDoubleClickedEvent(Vector2Int pos) => GridPosition = pos;
}

/// <summary>
/// 右键单击格子事件
/// </summary>
public class CellRightClickedEvent : GameEvent
{
    public Vector2Int GridPosition { get; }
    public CellRightClickedEvent(Vector2Int pos) => GridPosition = pos;
}

/// <summary>
/// 左键单击单位事件
/// </summary>
public class UnitLeftClickedEvent : GameEvent
{
    public Unit Unit { get; }
    public UnitLeftClickedEvent(Unit unit) => Unit = unit;
}

/// <summary>
/// 左键双击单位事件
/// </summary>
public class UnitDoubleClickedEvent : GameEvent
{
    public Unit Unit { get; }
    public UnitDoubleClickedEvent(Unit unit) => Unit = unit;
}

/// <summary>
/// ESC 键按下事件
/// </summary>
public class EscapePressedEvent : GameEvent
{
}

/// <summary>
/// 长按开始事件
/// </summary>
public class LongPressStartedEvent : GameEvent
{
    public ILongPressTarget Target { get; }
    public LongPressStartedEvent(ILongPressTarget target) => Target = target;
}

/// <summary>
/// 长按进度更新事件（每帧派发）
/// </summary>
public class LongPressUpdateEvent : GameEvent
{
    public ILongPressTarget Target { get; }
    public float Progress { get; }  // 0~1
    public LongPressUpdateEvent(ILongPressTarget target, float progress)
    {
        Target = target;
        Progress = progress;
    }
}

/// <summary>
/// 长按取消事件
/// </summary>
public class LongPressCancelledEvent : GameEvent
{
    public ILongPressTarget Target { get; }
    public LongPressCancelledEvent(ILongPressTarget target) => Target = target;
}

/// <summary>
/// 长按完成事件
/// </summary>
public class LongPressPerformedEvent : GameEvent
{
    public ILongPressTarget Target { get; }
    public LongPressPerformedEvent(ILongPressTarget target) => Target = target;
}