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
/// ESC 键按下事件
/// </summary>
public class EscapePressedEvent : GameEvent
{
}
