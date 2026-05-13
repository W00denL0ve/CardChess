using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋盘管理器，负责棋盘信息相关逻辑
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Config")]
    public float cellSize = 1f;
    public TerrainConfig terrainConfig;

    public LevelData CurrentLevel { get; private set; }
    public int TotalLayers => layers.Count;

    private List<Cell[,]> layers = new List<Cell[,]>();

    // 事件：格子逻辑更新，可视化组件监听刷新
    public event Action<int, int, int> OnCellUpdated;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 加载关卡数据并构建逻辑网格
    /// </summary>
    public void LoadLevel(LevelData levelData)
    {
        CurrentLevel = levelData;
        layers.Clear();

        for (int l = 0; l < levelData.TotalLayers; l++)
        {
            LayerData layerData = levelData.GetLayer(l);
            Cell[,] grid = new Cell[layerData.width, layerData.height];

            for (int col = 0; col < layerData.width; col++)
            {
                for (int row = 0; row < layerData.height; row++)
                {
                    CellData data = layerData.GetCell(col, row);
                    Cell cell = new Cell
                    {
                        col = col,
                        row = row,
                        layer = l,
                        terrainType = data.terrainType,
                        height = data.height,
                        isWalkable = terrainConfig != null ? terrainConfig.IsWalkable(data.terrainType) : true,
                        activeEffects = new List<Effect>()
                    };
                    grid[col, row] = cell;
                }
            }
            layers.Add(grid);
        }
    }

    /// <summary>
    /// 获取指定格子（建议5：统一col/row命名）
    /// </summary>
    public Cell GetCell(int col, int row, int layer = 0)
    {
        if (layer < 0 || layer >= layers.Count) return null;
        Cell[,] grid = layers[layer];
        if (col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1))
            return grid[col, row];
        return null;
    }

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int col, out int row, int layer = 0)
    {
        col = Mathf.RoundToInt(worldPos.x / cellSize);
        row = Mathf.RoundToInt(worldPos.z / cellSize);
        return GetCell(col, row, layer) != null;
    }

    /// <summary>
    /// 网格坐标转世界坐标（多层时Y轴偏移）
    /// </summary>
    public Vector3 GetWorldPosition(int col, int row, int layer = 0)
    {
        float yOffset = layer * cellSize;
        return new Vector3(col * cellSize, yOffset, row * cellSize);
    }

    /// <summary>
    /// 动态修改格子并触发可视化更新（建议6）
    /// </summary>
    public void SetCell(int col, int row, int layer, Action<Cell> updateAction)
    {
        Cell cell = GetCell(col, row, layer);
        if (cell == null) return;

        updateAction(cell);
        OnCellUpdated?.Invoke(col, row, layer);
    }

    // 便捷方法示例
    public void SetCellWalkable(int col, int row, int layer, bool walkable)
    {
        SetCell(col, row, layer, cell => cell.isWalkable = walkable);
    }

    public void AddCellEffect(int col, int row, int layer, Effect effect)
    {
        SetCell(col, row, layer, cell => cell.activeEffects.Add(effect));
    }
}