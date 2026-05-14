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

        Character charA = unitA.unit.GetComponent<Character>();
        Character charB = unitB.unit.GetComponent<Character>();
        if (charA == null || charB == null) return;

        Cell cellA = charA.currentCell;
        Cell cellB = charB.currentCell;
        if (cellA == null || cellB == null) return;

        // 交换位置
        charA.MoveTo(cellB);
        charB.MoveTo(cellA);
    }
}
