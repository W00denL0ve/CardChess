using System;
using System.Collections.Generic;
using System.Linq;
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

    public LevelGridData CurrentLevel { get; private set; }
    private Cell[,] grid;
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        GameEventChannel.Register<UnitMovedEvent>(HandleUnitMoved);
        GameEventChannel.Register<UnitDeathEvent>(HandleUnitDeath);
    }

    void OnDisable()
    {
        GameEventChannel.Unregister<UnitMovedEvent>(HandleUnitMoved);
        GameEventChannel.Unregister<UnitDeathEvent>(HandleUnitDeath);
    }

    /// <summary>
    /// 加载关卡数据并构建逻辑网格
    /// </summary>
    public void LoadGridData(LevelGridData levelData)
    {
        CurrentLevel = levelData;
        grid = new Cell[levelData.width, levelData.height];

        for (int col = 0; col < levelData.width; col++)
        {
            for (int row = 0; row < levelData.height; row++)
            {
            CellData data = levelData.GetCell(col, row);
            grid[col, row] = new()
            {
                col = col,
                row = row,
                terrainType = data.terrainType,
                height = data.height,
                isWalkable = terrainConfig?.IsWalkable(data.terrainType) ?? true,
                activeEffects = new List<Effect>()
            };
            }
        }
        gridVisualizer = gameObject.GetComponent<GridVisualizer>();
        gridVisualizer.RebuildAllVisuals();
    }

    /// <summary>
    /// 获取指定格子（建议5：统一col/row命名）
    /// </summary>
    public Cell GetCell(int col, int row)
    {
        if (grid == null) return null;
        if (col >= 0 && col < grid.GetLength(0) && row >= 0 && row < grid.GetLength(1))
            return grid[col, row];
        return null;
    }

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public bool WorldToGrid(Vector3 worldPos, out int col, out int row)
    {
        col = Mathf.RoundToInt(worldPos.x / cellSize);
        row = Mathf.RoundToInt(worldPos.z / cellSize);
        return GetCell(col, row) != null;
    }

    /// <summary>
    /// 网格坐标转世界坐标（多层时Y轴偏移）
    /// </summary>
    public Vector3 GetWorldPosition(int col, int row)
    {
        Cell cell = GetCell(col, row);
        float yOffset = cell != null ? cell.height * cellSize : 0;
        return new Vector3(col * cellSize, yOffset, row * cellSize);
    }

    /// <summary>
    /// 格子坐标转世界坐标（与 WorldToGrid 配对）
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return GetWorldPosition(gridPos.x, gridPos.y);
    }

    /// <summary>
    /// 格子坐标转世界坐标（与 WorldToGrid 配对）
    /// </summary>
    public Vector3 GridToWorld(int col, int row)
    {
        return GetWorldPosition(col, row);
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
        Cell cell = GetCell(col, row);
        if (cell == null) return;

        updateAction(cell);
        GameEventChannel.Dispatch(new CellUpdatedEvent(col, row));
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
    public void AddCellEffect(int col, int row, int layer, Effect effect)
    {
        SetCell(col, row, layer, cell => cell.activeEffects.Add(effect));
    }

    // ====================================================================
    //  Unit 相关
    // ====================================================================

    /// <summary>
    /// 放置单位到格子
    /// </summary>
    public void PlaceUnit(Unit unit, Vector2Int gridPos)
    {
        Cell cell = GetCell(gridPos.x, gridPos.y);
        if (cell == null || !cell.isWalkable || cell.OccupyingUnit != null) return;
        cell.OccupyingUnit = unit;
        unit.GridPosition = gridPos;
        GameEventChannel.Dispatch(new CellUpdatedEvent(cell));
    }

    private static readonly int[] dx = { 0, 1, 0, -1 };
    private static readonly int[] dy = { 1, 0, -1, 0 };

    /// <summary>根据上一步方向，返回 4 个方向的遍历顺序：同向优先 > 垂直次之 > 反向最后</summary>
    private static List<int> GetDirectionOrder(Vector2Int from, Vector2Int to)
    {
        Vector2Int dir = to - from;

        var scored = new (int index, int score)[4];
        for (int i = 0; i < 4; i++)
        {
            int score;
            if (dx[i] == dir.x && dy[i] == dir.y)
                score = 2;  // 同向
            else if (dx[i] == -dir.x && dy[i] == -dir.y)
                score = 0;  // 反向
            else
                score = 1;  // 垂直
            scored[i] = (i, score);
        }

        Array.Sort(scored, (a, b) => b.score.CompareTo(a.score));
        return scored.Select(s => s.index).ToList();
    }

    /// <summary>
    /// 寻路（BFS，曼哈顿距离）
    /// </summary>
    /// <param name="ignoreOccupied">忽略路径中间格子的占据单位</param>
    /// <param name="canPassUnwalkable">允许穿过不可行走格子，但终点必须可行走</param>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, 
        bool ignoreOccupied = false, bool canPassUnwalkable = false)
    {
        if (grid == null) return new List<Vector2Int>();
        if (start == end) return new List<Vector2Int> { start };
    
        // 终点不可行走时直接返回空（即使 canPassUnwalkable 为 true）
        Cell endCell = GetCell(end.x, end.y);
        if (endCell == null || !endCell.isWalkable) return new List<Vector2Int>();
    
        int w = grid.GetLength(0), h = grid.GetLength(1);
        var visited = new bool[w, h];
        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
    
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == end)
            {
                var path = new List<Vector2Int>();
                var node = end;
                while (node != start)
                {
                    path.Add(node);
                    node = parent[node];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            // 方向感知遍历：同向优先，使路径更直
            var order = (cur != start)
                ? GetDirectionOrder(parent[cur], cur)
                : new List<int> { 0, 1, 2, 3 };

            foreach (int i in order)
            {
                int nx = cur.x + dx[i], ny = cur.y + dy[i];
                var next = new Vector2Int(nx, ny);
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited[nx, ny]) continue;

                Cell cell = GetCell(nx, ny);
                if (cell == null) continue;

                // 可行走性检查：中间格子根据 canPassUnwalkable 决定
                if (!canPassUnwalkable && !cell.isWalkable) continue;
                // 占据检查：终点允许被占据，中间格子根据 ignoreOccupied 决定
                if (!ignoreOccupied && next != end && cell.OccupyingUnit != null) continue;

                visited[nx, ny] = true;
                parent[next] = cur;
                queue.Enqueue(next);
            }
        }
        return new List<Vector2Int>();
    }

    /// <summary>
    /// 可达区域（BFS，maxSteps 步内），返回不含起点的可行走格子列表
    /// </summary>
    /// <param name="ignoreOccupied">忽略格子的占据单位</param>
    /// <param name="canPassUnwalkable">允许穿过不可行走格子，但结果中只包含可行走的格子</param>
    public List<Vector2Int> GetReachableCells(Vector2Int start, int maxSteps,
        bool ignoreOccupied = false, bool canPassUnwalkable = false)
    {
        if (grid == null || maxSteps <= 0) return new List<Vector2Int>();

        int w = grid.GetLength(0), h = grid.GetLength(1);
        var reachable = new List<Vector2Int>();
        var visited = new bool[w, h];
        var queue = new Queue<(Vector2Int pos, int steps)>();

        queue.Enqueue((start, 0));
        visited[start.x, start.y] = true;

        while (queue.Count > 0)
        {
            var (cur, steps) = queue.Dequeue();
            if (steps > 0 && GetCell(cur.x, cur.y)?.isWalkable == true)
                reachable.Add(cur);

            if (steps >= maxSteps) continue;

            for (int i = 0; i < 4; i++)
            {
                int nx = cur.x + dx[i], ny = cur.y + dy[i];
                var next = new Vector2Int(nx, ny);
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                if (visited[nx, ny]) continue;

                Cell cell = GetCell(nx, ny);
                if (cell == null) continue;

                // 穿过规则
                if (!canPassUnwalkable && !cell.isWalkable) continue;
                // 占据规则
                if (!ignoreOccupied && cell.OccupyingUnit != null) continue;

                visited[nx, ny] = true;
                queue.Enqueue((next, steps + 1));
            }
        }
        return reachable;
    }

    // ====================================================================
    //  事件处理
    // ====================================================================

    private void HandleUnitMoved(UnitMovedEvent evt)
    {
        Cell targetCell = GetCell(evt.To.x, evt.To.y);
        if (targetCell == null) return;

        // 从原格子移除
        Cell fromCell = GetCell(evt.From.x, evt.From.y);
        if (fromCell != null)
        {
            if (fromCell.OccupyingUnit == evt.Unit)
                fromCell.OccupyingUnit = null;
        }

        // 放置到目标格子
        targetCell.OccupyingUnit = evt.Unit;

        GameEventChannel.Dispatch(new CellUpdatedEvent(targetCell));
        if (fromCell != null)
            GameEventChannel.Dispatch(new CellUpdatedEvent(fromCell));
    }

    private void HandleUnitDeath(UnitDeathEvent evt)
    {
        Cell cell = GetCell(evt.DeathPosition.x, evt.DeathPosition.y);
        if (cell != null && cell.OccupyingUnit == evt.Unit)
        {
            cell.OccupyingUnit = null;
            GameEventChannel.Dispatch(new CellUpdatedEvent(cell));
        }
    }
}