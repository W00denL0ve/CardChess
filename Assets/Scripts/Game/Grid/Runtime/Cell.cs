using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单元格运行时类，包含单元格的所有信息
/// </summary>
public class Cell
{
    public int col;
    public int row;
    public TerrainType terrainType;
    public int height;
    public bool isWalkable;
    public List<Effect> activeEffects;
    public Unit OccupyingUnit;
}