using UnityEngine;

/// <summary>
/// 格子目标 - 包装一个格子坐标
/// </summary>
public class CellTarget : ITarget
{
    public Vector2Int coord;

    public CellTarget(Vector2Int c) => coord = c;

    public Vector3? GetWorldPosition() => GridManager.Instance?.GetWorldPosition(coord.x, coord.y);

    public Vector2Int? GetCellPosition() => coord;

    public GameObject gameObject => null; // 格子可能没有独立 GameObject
}
