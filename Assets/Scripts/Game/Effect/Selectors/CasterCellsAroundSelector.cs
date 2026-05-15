using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 返回以施法者为中心的半径内所有格子
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/CasterCellsAround")]
public class CasterCellsAroundSelector : TargetSelector
{
    /// <summary>半径（格数）</summary>
    public int radius = 1;

    /// <summary>是否只返回可行走的格子</summary>
    public bool onlyWalkable;

    public override List<ITarget> GetTargets(EffectContext context)
    {
        Unit casterUnit = context.caster?.GetComponent<Unit>();
        if (casterUnit == null) return new List<ITarget>();

        Vector2Int center = casterUnit.GridPosition;

        List<ITarget> cells = new List<ITarget>();
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);

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
