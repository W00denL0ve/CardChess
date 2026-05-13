/// <summary>
/// 地图格数据，地形种类，高度（可拓展）
/// </summary>
[System.Serializable]
public struct CellData
{
    public TerrainType terrainType;
    public int height;
}