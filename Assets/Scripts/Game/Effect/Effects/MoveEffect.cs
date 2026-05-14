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
        // 源必须是单位，目标必须是格子
        UnitTarget unitTarget = source as UnitTarget;
        CellTarget cellTarget = target as CellTarget;
        if (unitTarget?.unit == null || cellTarget == null) return;

        Character character = unitTarget.unit.GetComponent<Character>();
        if (character == null) return;

        // 获取目标格子对象
        Cell destCell = GridManager.Instance?.GetCell(cellTarget.coord.x, cellTarget.coord.y);
        if (destCell == null) return;

        // 如果要求路径可达，检查目的地是否可达
        if (requirePath)
        {
            // 暂时直接移动，后续可以接入 Pathfinding 系统
        }

        // 执行移动
        character.MoveTo(destCell);
    }
}