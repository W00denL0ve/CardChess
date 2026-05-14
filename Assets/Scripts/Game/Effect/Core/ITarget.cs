using UnityEngine;

/// <summary>
/// 可作用目标接口 - 所有可以被效果作用的对象都要实现此接口
/// </summary>
public interface ITarget
{
    /// <summary>获取该目标当前的世界坐标（如果适用）</summary>
    Vector3? GetWorldPosition();

    /// <summary>获取该目标所在的格子坐标（如果适用）</summary>
    Vector2Int? GetCellPosition();

    /// <summary>获取目标的 GameObject（用于显示、特效等）</summary>
    GameObject gameObject { get; }
}
