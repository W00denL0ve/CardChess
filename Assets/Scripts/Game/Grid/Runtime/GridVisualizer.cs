using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋盘视觉管理器，从棋盘管理器剥离出的，单独负责棋盘视觉显示
/// </summary>
public class GridVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject cellVisualPrefab;
    public float visualScaleFactor = 0.9f;
    public float visualHeight = 0.1f;

    [Header("Highlight Materials")]
    [SerializeField] private Material highlightMaterial3D;
    [SerializeField] private Material selectedMaterial3D;
    [SerializeField] private Material highlightMaterial2D;
    [SerializeField] private Material defaultMaterial2D;

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

    // 缓存每个格子视觉对象的原始材质
    private Dictionary<string, Material> originalMaterials = new Dictionary<string, Material>();

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
    /// 获取格子视觉对象的渲染器组件（支持 MeshRenderer 和 SpriteRenderer）
    /// </summary>
    Renderer GetCellRenderer(GameObject visual)
    {
        if (visual == null) return null;
        Renderer renderer = visual.GetComponent<MeshRenderer>();
        if (renderer != null) return renderer;
        return visual.GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 根据渲染器类型应用对应材质
    /// </summary>
    void ApplyMaterial(GameObject visual, Material mat)
    {
        if (visual == null)
        {
            Logger.LogWarning("GridVisualizer: 试图为空物体应用材质");
            return;
        }
        if (mat == null)
        {
            Logger.LogWarning("GridVisualizer: 未找到材质");
            return;
        }

        Renderer renderer = GetCellRenderer(visual);
        if (renderer != null)
        {
            renderer.material = mat;
        }
        else
        {
            Logger.LogWarning($"GridVisualizer: {visual.name} 没有 MeshRenderer 或 SpriteRenderer");
        }
    }

    /// <summary>
    /// 根据渲染器类型获取当前材质
    /// </summary>
    Material GetCurrentMaterial(GameObject visual)
    {
        Renderer renderer = GetCellRenderer(visual);
        return renderer != null ? renderer.sharedMaterial : null;
    }

    /// <summary>
    /// 高亮指定格子列表（跳过被占据的格子），并保存原始材质
    /// </summary>
    public void HighlightCells(List<Vector2Int> positions)
    {
        foreach (var pos in positions)
        {
            Cell cell = GridManager.Instance.GetCell(pos.x, pos.y);
            if (cell == null) continue;
            // 只高亮未被占据的格子
            if (cell.OccupyingUnit != null) continue;

            GameObject visual = FindVisualCube(pos.x, pos.y);
            if (visual == null) continue;

            string key = $"Cell_{pos.x}_{pos.y}";
            // 保存原始材质（仅首次）
            if (!originalMaterials.ContainsKey(key))
            {
                Material original = GetCurrentMaterial(visual);
                if (original != null)
                    originalMaterials[key] = original;
            }

            // 根据渲染器类型应用对应高亮材质
            Renderer renderer = GetCellRenderer(visual);
            if (renderer is SpriteRenderer)
                ApplyMaterial(visual, highlightMaterial2D);
            else
                ApplyMaterial(visual, highlightMaterial3D ?? hightlightMat);
        }
        Logger.Log($"GridVisualizer: 高亮 {positions.Count} 个格子");
    }

    /// <summary>
    /// 清除所有高亮，恢复原始材质
    /// </summary>
    public void ClearHighlights()
    {
        foreach (var kvp in originalMaterials)
        {
            GameObject visual = FindVisualCubeByName(kvp.Key);
            if (visual != null)
                ApplyMaterial(visual, kvp.Value);
        }
        originalMaterials.Clear();
        Logger.Log("GridVisualizer: 已清除所有高亮");
    }

    /// <summary>
    /// 根据给定的坐标列表设置格子为选中状态
    /// </summary>
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
    /// 根据完整的物体名查找视觉对象
    /// </summary>
    GameObject FindVisualCubeByName(string targetName)
    {
#if USE_DYNAMIC_VISIBILITY
        return null; // 动态模式下暂不支持
#else
        foreach (var cube in allCubes)
        {
            if (cube.name == targetName)
                return cube;
        }
        return null;
#endif
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