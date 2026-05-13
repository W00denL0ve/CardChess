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

    private GridVisualizer gridVisualizer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 加载关卡数据并构建逻辑网格
    /// </summary>
    public void LoadLevelData(LevelData levelData)
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
        gridVisualizer = FindAnyObjectByType<GridVisualizer>();
        gridVisualizer.RebuildAllVisuals();
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
    /// 动态修改格子并触发可视化更新
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="layer"></param>
    /// <param name="updateAction"></param>
    public void SetCell(int col, int row, int layer, Action<Cell> updateAction)
    {
        Cell cell = GetCell(col, row, layer);
        if (cell == null) return;

        updateAction(cell);
        GameEventChannel.Dispatch(new CellUpdatedEvent(col, row, layer));
    }

    /// <summary>
    /// 触发某个格子的效果
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="layer"></param>
    public void TriggerCellEffect(int col, int row, int layer)
    {
        // todo
    }



    // 便捷方法示例

    /// <summary>
    /// 便捷方法：设置一个格子是否可达
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="layer"></param>
    /// <param name="walkable"></param>
    public void SetCellWalkable(int col, int row, int layer, bool walkable)
    {
        SetCell(col, row, layer, cell => cell.isWalkable = walkable);
    }

    /// <summary>
    /// 便捷方法：为一个格子增加效果
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <param name="layer"></param>
    /// <param name="effect"></param>
    public void AddCellEffect(int col, int row, int layer, Effect effect)
    {
        SetCell(col, row, layer, cell => cell.activeEffects.Add(effect));
    }
}