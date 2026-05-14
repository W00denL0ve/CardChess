using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 返回以 anchor1 为中心的半径内所有格子
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CellsAroundAnchor1")]
public class CellsAroundAnchor1Selector : TargetSelector
{
    /// <summary>半径（格数）</summary>
    public int radius = 1;

    /// <summary>是否只返回可行走的格子</summary>
    public bool onlyWalkable;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        var center = context.anchor1?.GetCellPosition();
        if (center == null) return new List<ITarget>();

        List<ITarget> cells = new List<ITarget>();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int pos = new Vector2Int(center.Value.x + dx, center.Value.y + dy);

                if (onlyWalkable && GridManager.Instance != null)
                {
                    Cell cell = GridManager.Instance.GetCell(pos.x, pos.y);
                    if (cell == null || !cell.isWalkable) continue;
                }

                cells.Add(new CellTarget(pos));
            }
        }
        return cells;
    }
}
