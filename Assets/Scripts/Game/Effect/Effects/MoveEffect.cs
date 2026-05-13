using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveEffect", menuName = "Effects/MoveEffect")]
public class MoveEffect : Effect
{
    public override void Execute(EffectContext context)
    {
        // List<Cell> movableCells = GridManager.Instance.GetMovableCells(context.target, destination, steps);
        // InputManager.Instance.WaitForCellSelection(selectedCell =>
        // {
        //     context.target.MoveTo(selectedCell);
        // }, movableCells);
    }
}