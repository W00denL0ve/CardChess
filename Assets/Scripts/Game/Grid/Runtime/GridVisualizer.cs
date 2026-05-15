using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 棋盘视觉管理器，从棋盘管理器剥离出的，单独负责棋盘视觉显示
/// </summary>
public class GridVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject cellVisualPrefab;
    public float visualScaleFactor = 0.9f;
    public float visualHeight = 0.1f;

    [Header("Dynamic Visibility (Optional, requires USE_DYNAMIC_VISIBILITY)")]
    public Camera targetCamera;
    public int poolInitialSize = 200;
    public float visibilityMargin = 1.5f;

#if USE_DYNAMIC_VISIBILITY
    private Queue<GameObject> cubePool;
    private List<GameObject> activeCubes;
    private GameObject[,] visualObjects;
#else
    private List<GameObject> allCubes = new List<GameObject>();
#endif

    private Material hightlightMat;
    private Material selectedMat;

    void Awake()
    {
        GameEventChannel.Register<CellUpdatedEvent>(OnCellUpdated);
        Logger.Log("GridVisualizer：已订阅格子更新事件");
    }
    void Start()
    {
        hightlightMat = GridManager.Instance.terrainConfig.hightlightMat;
        selectedMat = GridManager.Instance.terrainConfig.selectedMat;
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<CellUpdatedEvent>(OnCellUpdated);
    }

    /// <summary>
    /// 加载新关卡时调用，重建所有可视化
    /// </summary>
    public void RebuildAllVisuals()
    {
        ClearAllVisuals();
        if (GridManager.Instance.CurrentLevel == null) return;

#if USE_DYNAMIC_VISIBILITY
        InitializeVisualLayers();
#else
        GenerateAllCubes();
#endif
    }

    void GenerateAllCubes()
    {
        for (int col = 0; col < GridManager.Instance.CurrentLevel.width; col++)
        {
            for (int row = 0; row < GridManager.Instance.CurrentLevel.height; row++)
            {
                GameObject cube = CreateCube(col, row);
                allCubes.Add(cube);
            }
        }
    }

    GameObject CreateCube(int col, int row)
    {
        Vector3 worldPos = GridManager.Instance.GetWorldPosition(col, row);
        GameObject cube = Instantiate(cellVisualPrefab, worldPos, Quaternion.identity, transform);

        float cellSize = GridManager.Instance.cellSize;
        cube.transform.localScale = new Vector3(
            cellSize * visualScaleFactor,
            visualHeight,
            cellSize * visualScaleFactor
        );
        cube.name = $"Cell_{col}_{row}";

        ApplyTerrainMaterial(cube, col, row);
        return cube;
    }

    /// <summary>
    /// 为格子立方体根据格子类型应用材质
    /// </summary>
    /// <param name="cube"></param>
    /// <param name="col"></param>
    /// <param name="row"></param>
    void ApplyTerrainMaterial(GameObject cube, int col, int row)
    {
        Cell cell = GridManager.Instance.GetCell(col, row);
        if (cell == null) return;

        Material mat = GridManager.Instance.terrainConfig?.GetMaterial(cell.terrainType);
        ApplyMaterial(cube, mat);
    }

    /// <summary>
    /// 工具方法，为格子立方体应用指定材质
    /// </summary>
    /// <param name="cube"></param>
    /// <param name="mat"></param>
    void ApplyMaterial(GameObject cube, Material mat)
    {
        if (cube != null)
        {
            if (mat != null)
            {
                cube.GetComponent<Renderer>().material = mat;
                return;
            }
            Logger.LogWarning("GridVisualizer: 未找到材质");
            return;
        }
        Logger.LogWarning("GridVisualizer: 试图为空物体应用材质");
    }

    /// <summary>
    /// 根据给定坐标列表高亮格子
    /// </summary>
    /// <param name="positions">坐标列表</param>
    public void HightlightCells(List<Vector2Int> positions)
    {
        if (hightlightMat != null)
        {
            foreach (Vector2Int position in positions)
            {
                GameObject cube = FindVisualCube(position.x, position.y);
                ApplyMaterial(cube, hightlightMat);
            }
            return;
        }
        Logger.LogWarning("GridVisualizer: 未定义高亮材质");
    }

    /// <summary>
    /// 根据给定的坐标列表设置格子为选中状态
    /// </summary>
    /// <param name="positions"></param>
    public void SetSelectedCells(List<Vector2Int> positions)
    {
        if (selectedMat != null)
        {
            foreach (Vector2Int position in positions)
            {
                GameObject cube = FindVisualCube(position.x, position.y);
                ApplyMaterial(cube, selectedMat);
            }
            return;
        }
        Logger.LogWarning("GridVisualizer: 未定义高亮材质");
    }

    /// <summary>
    /// 单个格子更新回调
    /// </summary>
    void OnCellUpdated(CellUpdatedEvent evt)
    {
        Logger.Log("收到格子更新事件");
        GameObject cube = FindVisualCube(evt.col, evt.row);
        if (cube != null)
            ApplyTerrainMaterial(cube, evt.col, evt.row);
    }

    GameObject FindVisualCube(int col, int row)
    {
        string targetName = $"Cell_{col}_{row}";
#if USE_DYNAMIC_VISIBILITY
        if (layer < visualLayers.Count && visualLayers[layer] != null)
            return visualLayers[layer][col, row];
#else
        foreach (var cube in allCubes)
        {
            if (cube.name == targetName)
                return cube;
        }
#endif
        return null;
    }

    void ClearAllVisuals()
    {
#if USE_DYNAMIC_VISIBILITY
        foreach (var cube in activeCubes) Destroy(cube);
        activeCubes.Clear();
        cubePool.Clear();
        visualLayers.Clear();
#else
        foreach (var cube in allCubes) Destroy(cube);
        allCubes.Clear();
#endif
    }

#if USE_DYNAMIC_VISIBILITY
    void InitializeVisualLayers()
    {
        visualLayers = new List<GameObject[,]>();
        for (int l = 0; l < GridManager.Instance.TotalLayers; l++)
        {
            int w = GridManager.Instance.CurrentLevel.GetLayer(l).width;
            int h = GridManager.Instance.CurrentLevel.GetLayer(l).height;
            visualLayers.Add(new GameObject[w, h]);
        }

        cubePool = new Queue<GameObject>();
        for (int i = 0; i < poolInitialSize; i++)
        {
            GameObject cube = Instantiate(cellVisualPrefab, Vector3.zero, Quaternion.identity, transform);
            cube.SetActive(false);
            cubePool.Enqueue(cube);
        }
    }

    void LateUpdate()
    {
        if (GridManager.Instance.CurrentLevel == null || targetCamera == null) return;
        UpdateVisualsFromCamera();
    }

    void UpdateVisualsFromCamera()
    {
        // 获取相机大致视野范围
        float camHeight = targetCamera.transform.position.y;
        float verticalFov = targetCamera.fieldOfView;
        float horizontalFov = Camera.VerticalToHorizontalFieldOfView(verticalFov, targetCamera.aspect);
        float halfWidth = Mathf.Tan(Mathf.Deg2Rad * horizontalFov * 0.5f) * camHeight;
        float halfHeight = Mathf.Tan(Mathf.Deg2Rad * verticalFov * 0.5f) * camHeight;
        
        Vector3 camPos = targetCamera.transform.position;
        float cellSize = GridManager.Instance.cellSize;
        float margin = visibilityMargin * cellSize;

        int minCol = Mathf.FloorToInt((camPos.x - halfWidth - margin) / cellSize);
        int maxCol = Mathf.CeilToInt((camPos.x + halfWidth + margin) / cellSize);
        int minRow = Mathf.FloorToInt((camPos.z - halfHeight - margin) / cellSize);
        int maxRow = Mathf.CeilToInt((camPos.z + halfHeight + margin) / cellSize);

        // 简化：只在第0层做动态显示，如需要多层，遍历所有层
        int layer = 0;
        LayerData layerData = GridManager.Instance.CurrentLevel.GetLayer(layer);
        if (layerData == null) return;

        minCol = Mathf.Clamp(minCol, 0, layerData.width - 1);
        maxCol = Mathf.Clamp(maxCol, 0, layerData.width - 1);
        minRow = Mathf.Clamp(minRow, 0, layerData.height - 1);
        maxRow = Mathf.Clamp(maxRow, 0, layerData.height - 1);

        HashSet<Vector2Int> desired = new HashSet<Vector2Int>();
        for (int c = minCol; c <= maxCol; c++)
            for (int r = minRow; r <= maxRow; r++)
                desired.Add(new Vector2Int(c, r));

        // 回收不需要的
        List<GameObject> toRecycle = new List<GameObject>();
        foreach (var cube in activeCubes)
        {
            string[] parts = cube.name.Replace("Cell_L0_", "").Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int cc) && int.TryParse(parts[1], out int rr))
            {
                if (!desired.Contains(new Vector2Int(cc, rr)))
                    toRecycle.Add(cube);
            }
        }
        foreach (var cube in toRecycle)
        {
            string[] parts = cube.name.Replace("Cell_L0_", "").Split('_');
            int cc = int.Parse(parts[0]), rr = int.Parse(parts[1]);
            if (visualLayers[layer] != null)
                visualLayers[layer][cc, rr] = null;
            cube.SetActive(false);
            cubePool.Enqueue(cube);
            activeCubes.Remove(cube);
        }

        // 生成新的
        foreach (var coord in desired)
        {
            if (visualLayers[layer] != null && visualLayers[layer][coord.x, coord.y] == null)
            {
                GameObject cube = GetPooledCube();
                cube.transform.position = GridManager.Instance.GetWorldPosition(coord.x, coord.y, layer);
                cube.transform.localScale = new Vector3(cellSize * visualScaleFactor, visualHeight, cellSize * visualScaleFactor);
                ApplyTerrainMaterial(cube, coord.x, coord.y, layer);
                cube.name = $"Cell_L{layer}_{coord.x}_{coord.y}";
                cube.SetActive(true);
                visualLayers[layer][coord.x, coord.y] = cube;
                activeCubes.Add(cube);
            }
        }
    }

    GameObject GetPooledCube()
    {
        if (cubePool.Count > 0)
            return cubePool.Dequeue();
        return Instantiate(cellVisualPrefab, Vector3.zero, Quaternion.identity, transform);
    }
#endif
}