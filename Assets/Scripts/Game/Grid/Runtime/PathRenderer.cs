using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径绘制组件 - 使用对象池管理路径上的线段和箭头精灵
/// </summary>
public class PathRenderer : MonoBehaviour
{
    public static PathRenderer Instance { get; private set; }

    void Awake() { Instance = this; }

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
        if (path == null)
        {
            Logger.LogWarning("PathRenderer: 路径为空");
            return;
        }
        if (path.Count < 2)
        {
            // 仅起点（原地不动），无需绘制
            return;
        }

        GridManager grid = GridManager.Instance;
        if (grid == null) return;

        // 绘制线段：除最后一段外，每两个连续格子之间放一条线段
        // 最后一段用箭头取代
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 from = grid.GridToWorld(path[i]);
            Vector3 to = grid.GridToWorld(path[i + 1]);
            Vector3 mid = (from + to) * 0.5f;
            Vector3 dir = (to - from).normalized;

            // 最后一段用箭头取代线段
            if (i == path.Count - 2)
            {
                GameObject arrow = GetArrow();
                if (arrow != null)
                {
                    arrow.transform.position = mid;
                    // 预制体位于 XY 平面指向 +X，需平躺到 XZ 平面
                    // 用 LookRotation 构造：+Z 朝上（平躺）、+X 指向 dir
                    Vector3 up = Vector3.up;
                    Vector3 dirXZ = dir;
                    Vector3 spriteUp = Vector3.Cross(up, dirXZ);
                    arrow.transform.rotation = Quaternion.LookRotation(up, spriteUp);
                }
            }
            else
            {
                GameObject line = GetLine();
                if (line == null) continue;

                line.transform.position = mid;
                Vector3 up = Vector3.up;
                Vector3 dirXZ = dir;
                Vector3 spriteUp = Vector3.Cross(up, dirXZ);
                line.transform.rotation = Quaternion.LookRotation(up, spriteUp);
            }
        }

        Logger.Log($"PathRenderer: 显示路径 ({path.Count} 个格子)");
    }

    /// <summary>
    /// 隐藏所有路径精灵
    /// </summary>
    public void HidePath()
    {
        // Debug.Log($"[Path] HidePath (lines:{activeLines.Count}, arrows:{activeArrows.Count})");
        foreach (var go in activeLines.ToArray())
            Recycle(go, linePool, activeLines);
        foreach (var go in activeArrows.ToArray())
            Recycle(go, arrowPool, activeArrows);
    }
}
