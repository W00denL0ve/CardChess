using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径绘制组件 - 使用对象池管理路径上的线段和箭头精灵
/// </summary>
public class PathRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject arrowPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 20;

    private Queue<GameObject> linePool = new Queue<GameObject>();
    private Queue<GameObject> arrowPool = new Queue<GameObject>();
    private List<GameObject> activeLines = new List<GameObject>();
    private List<GameObject> activeArrows = new List<GameObject>();

    void Start()
    {
        InitializePool();
    }

    /// <summary>
    /// 预实例化对象池
    /// </summary>
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateLineObject(false);
            CreateArrowObject(false);
        }
        Logger.Log($"PathRenderer: 对象池初始化完成 (线段:{poolSize}, 箭头:{poolSize})");
    }

    GameObject CreateLineObject(bool active)
    {
        if (linePrefab == null) return null;
        GameObject go = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        go.SetActive(active);
        linePool.Enqueue(go);
        return go;
    }

    GameObject CreateArrowObject(bool active)
    {
        if (arrowPrefab == null) return null;
        GameObject go = Instantiate(arrowPrefab, Vector3.zero, Quaternion.identity, transform);
        go.SetActive(active);
        arrowPool.Enqueue(go);
        return go;
    }

    /// <summary>
    /// 从对象池获取一个线段精灵
    /// </summary>
    GameObject GetLine()
    {
        if (linePool.Count == 0)
            CreateLineObject(false);
        GameObject go = linePool.Dequeue();
        go.SetActive(true);
        activeLines.Add(go);
        return go;
    }

    /// <summary>
    /// 从对象池获取一个箭头精灵
    /// </summary>
    GameObject GetArrow()
    {
        if (arrowPool.Count == 0)
            CreateArrowObject(false);
        GameObject go = arrowPool.Dequeue();
        go.SetActive(true);
        activeArrows.Add(go);
        return go;
    }

    /// <summary>
    /// 回收单个对象到池中
    /// </summary>
    void Recycle(GameObject go, Queue<GameObject> pool, List<GameObject> activeList)
    {
        if (go == null) return;
        go.SetActive(false);
        activeList.Remove(go);
        pool.Enqueue(go);
    }

    /// <summary>
    /// 显示路径
    /// </summary>
    public void ShowPath(List<Vector2Int> path)
    {
        HidePath();
        if (path == null || path.Count < 2)
        {
            Logger.LogWarning("PathRenderer: 路径为空或长度不足");
            return;
        }

        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        // 绘制线段：每两个连续格子之间
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 from = grid.GridToWorld(path[i]);
            Vector3 to = grid.GridToWorld(path[i + 1]);
            Vector3 mid = (from + to) * 0.5f;

            GameObject line = GetLine();
            if (line == null) continue;

            line.transform.position = mid;
            // 旋转指向下一格（绕 Y 轴）
            Vector3 dir = to - from;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            line.transform.rotation = Quaternion.Euler(0, angle, 0);
        }

        // 最后一个格子放置箭头
        if (path.Count >= 2)
        {
            Vector3 lastPos = grid.GridToWorld(path[path.Count - 1]);
            Vector3 prevPos = grid.GridToWorld(path[path.Count - 2]);
            Vector3 dir = lastPos - prevPos;
            float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            GameObject arrow = GetArrow();
            if (arrow != null)
            {
                arrow.transform.position = lastPos;
                arrow.transform.rotation = Quaternion.Euler(0, angle, 0);
            }
        }

        Logger.Log($"PathRenderer: 显示路径 ({path.Count} 个格子)");
    }

    /// <summary>
    /// 隐藏所有路径精灵
    /// </summary>
    public void HidePath()
    {
        foreach (var go in activeLines.ToArray())
            Recycle(go, linePool, activeLines);
        foreach (var go in activeArrows.ToArray())
            Recycle(go, arrowPool, activeArrows);
    }
}
