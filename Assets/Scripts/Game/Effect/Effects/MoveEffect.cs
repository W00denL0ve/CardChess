using UnityEngine;

/// <summary>
/// 移动效果 - 将源单位移动到目标格子
/// </summary>
[CreateAssetMenu(fileName = "MoveEffect", menuName = "Game/Effect/Move")]
public class MoveEffect : Effect
{
    /// <summary>是否需要路径可达</summary>
    public bool requirePath = true;

    protected override void ApplyToPair(ITarget source, ITarget target, EffectContext context)
    {
        UnitTarget unitTarget = source as UnitTarget;
        CellTarget cellTarget = target as CellTarget;
        if (unitTarget?.unit == null || cellTarget == null) return;

        if (!unitTarget.unit.IsAlive) return;

        Cell destCell = GridManager.Instance?.GetCell(cellTarget.coord.x, cellTarget.coord.y);
        if (destCell == null || !destCell.isWalkable) return;

        if (requirePath)
        {
            var path = GridManager.Instance.FindPath(unitTarget.unit.GridPosition, cellTarget.coord);
            if (path == null) return;
        }

        unitTarget.unit.RequestMove(cellTarget.coord, context);
    }
}