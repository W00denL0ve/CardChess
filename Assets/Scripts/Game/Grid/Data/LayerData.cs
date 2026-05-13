/// <summary>
/// 层数据，方便后续拓展分层关卡
/// </summary>
[System.Serializable]
public class LayerData
{
    public int width;
    public int height;
    public CellData[] cells;

    public CellData GetCell(int col, int row)
    {
        int index = row * width + col;
        if (index >= 0 && index < cells.Length)
            return cells[index];
        return new CellData { terrainType = TerrainType.ground, height = 0 };
    }
}