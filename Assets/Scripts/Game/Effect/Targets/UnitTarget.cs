using UnityEngine;

/// <summary>
/// 单位目标 - 包装一个 Unit（角色、敌人等）
/// </summary>
public class UnitTarget : ITarget
{
    public Unit unit;

    public UnitTarget(Unit u) => unit = u;

    public Vector3? GetWorldPosition() => unit?.transform.position;

    public Vector2Int? GetCellPosition() => unit?.GetComponent<Character>()?.currentCell != null
        ? new Vector2Int?(new Vector2Int(
            unit.GetComponent<Character>().currentCell.col,
            unit.GetComponent<Character>().currentCell.row))
        : null;

    public GameObject gameObject => unit?.gameObject;
}
