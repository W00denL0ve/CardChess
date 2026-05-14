using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 地形预设资产
/// </summary>
[CreateAssetMenu(fileName = "TerrainConfig", menuName = "Game/TerrainConfig")]
public class TerrainConfig : ScriptableObject
{
    [System.Serializable]
    public struct TerrainEntry
    {
        public TerrainType type;
        public bool defaultWalkable;
        public Material visualMaterial;
    }

    public TerrainEntry[] entries;

    [Header("高亮材质")]
    public Material hightlightMat;

    [Header("选中材质")]
    public Material selectedMat;

    public Material GetMaterial(TerrainType type)
    {
        foreach (var entry in entries)
            if (entry.type == type)
                return entry.visualMaterial;
        return null;
    }

    public bool IsWalkable(TerrainType type)
    {
        foreach (var entry in entries)
            if (entry.type == type)
                return entry.defaultWalkable;
        return false;
    }
}