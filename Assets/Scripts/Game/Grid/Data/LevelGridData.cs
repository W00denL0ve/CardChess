using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡格子数据资源类，LevelData子资产
/// </summary>
[CreateAssetMenu(fileName = "LevelGridData", menuName = "CardChess/Levels/LevelGridData")]
public class LevelGridData : ScriptableObject
{
    public int width;
    public int height;
    public CellData[] cells; // 一维数组，索引 = row * width + col

    public CellData GetCell(int col, int row)
    {
        int index = row * width + col;
        if (index >= 0 && index < cells.Length)
            return cells[index];
        return new CellData { terrainType = TerrainType.unreachable, height = 0 };
    }

    public void SetGridData(int w, int h, CellData[] data)
    {
        width = w;
        height = h;
        cells = data;
    }
}