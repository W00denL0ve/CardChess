using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡数据资源类
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    public List<LayerData> layers = new List<LayerData>();

    public int TotalLayers => layers.Count;

    public LayerData GetLayer(int index)
    {
        if (index >= 0 && index < layers.Count)
            return layers[index];
        return null;
    }

    public void AddLayer(int width, int height, CellData[] cells)
    {
        layers.Add(new LayerData
        {
            width = width,
            height = height,
            cells = cells
        });
    }
}