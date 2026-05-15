using UnityEngine;

/// <summary>
/// 交换效果 - 将源单位与目标单位交换位置
/// </summary>
[CreateAssetMenu(fileName = "SwapEffect", menuName = "Game/Effect/Swap")]
public class SwapEffect : Effect
{
    protected override void ApplyToPair(ITarget source, ITarget target, EffectContext context)
    {
        UnitTarget unitA = source as UnitTarget;
        UnitTarget unitB = target as UnitTarget;
        if (unitA?.unit == null || unitB?.unit == null) return;
        if (!unitA.unit.IsAlive || !unitB.unit.IsAlive) return;

        Vector2Int posA = unitA.unit.GridPosition;
        Vector2Int posB = unitB.unit.GridPosition;

        unitA.unit.RequestMove(posB, context);
        unitB.unit.RequestMove(posA, context);
    }
}
