using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 浮字管理器：单例，负责对象池、合并计时、位置堆叠、生命周期控制
/// 依赖：GridManager 需提供 GridToWorld(Vector2Int) 方法
/// </summary>
public class FloatingNumberManager : MonoBehaviour
{
    public static FloatingNumberManager Instance;

    [Header("对象池设置")]
    [SerializeField] private GameObject floatingNumberPrefab;   // 浮字预制体
    [SerializeField] private int poolSize = 30;                 // 初始池容量

    [Header("显示位置")]
    [SerializeField] private Vector3 fixedOffset = new Vector3(0, 0.5f, 0); // 格子中心的固定偏移
    [SerializeField] private float stackOffsetY = 0.4f;         // 普通浮字垂直堆叠间距（向上为正）

    [Header("合并窗口（仅普通伤害/治疗）")]
    [SerializeField] private float mergeWindow = 0.5f;          // 合并时间窗口（秒）

    // 普通浮字数据结构：每个格子一个列表，支持合并和堆叠
    private Dictionary<Vector2Int, List<ActiveFloatingNumber>> activeByCell = new Dictionary<Vector2Int, List<ActiveFloatingNumber>>();

    // 对象池
    private Queue<FloatingNumber> pool = new Queue<FloatingNumber>();

    // 活跃浮字的运行时数据（仅用于普通浮字）
    private class ActiveFloatingNumber
    {
        public FloatingNumber number;       // 浮字组件
        public FloatingNumberType type;     // 类型
        public float remainingMergeTime;    // 剩余合并窗口时间（<=0 时触发飘走）
        public int currentValue;            // 当前累加数值
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() => InitPool();

    /// <summary>
    /// 初始化对象池，预生成实例
    /// </summary>
    private void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var obj = Instantiate(floatingNumberPrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj.GetComponent<FloatingNumber>());
        }
    }

    /// <summary>
    /// 从池中获取浮字（无空闲则动态创建）
    /// </summary>
    private FloatingNumber GetFromPool()
    {
        if (pool.Count == 0)
        {
            var obj = Instantiate(floatingNumberPrefab, transform);
            return obj.GetComponent<FloatingNumber>();
        }
        var fn = pool.Dequeue();
        fn.gameObject.SetActive(true);
        return fn;
    }

    /// <summary>
    /// 回收浮字到池中
    /// </summary>
    private void ReturnToPool(FloatingNumber fn)
    {
        fn.ForceRecycle();
        fn.gameObject.SetActive(false);
        pool.Enqueue(fn);
    }

    // ==================== 普通显示（物理/魔法/治疗） ====================
    /// <summary>
    /// 显示普通浮字（支持合并和堆叠）
    /// </summary>
    /// <param name="cellPos">格子坐标（二维）</param>
    /// <param name="value">数值</param>
    /// <param name="type">类型</param>
    public void ShowNumber(Vector2Int cellPos, int value, FloatingNumberType type)
    {
        // 获取或创建该格子的活跃列表
        if (!activeByCell.TryGetValue(cellPos, out var list))
        {
            list = new List<ActiveFloatingNumber>();
            activeByCell[cellPos] = list;
        }

        // 查找同类型且未飘走的浮字用于合并
        ActiveFloatingNumber existing = null;
        foreach (var afn in list)
        {
            if (afn.type == type && !afn.number.IsFloatingAway)
            {
                existing = afn;
                break;
            }
        }

        if (existing != null)
        {
            // 合并：累加数值，更新显示，播放脉冲，重置计时器
            existing.currentValue += value;
            existing.number.SetValue(existing.currentValue, type);
            existing.number.PlayMergePulse();
            existing.remainingMergeTime = mergeWindow;
        }
        else
        {
            // 清理同类型但正在飘走的浮字（强制回收，为新浮字让路）
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var afn = list[i];
                if (afn.type == type && afn.number.IsFloatingAway)
                {
                    ReturnToPool(afn.number);
                    list.RemoveAt(i);
                }
            }

            // 计算新浮字的世界坐标
            Vector3 worldPos = GridManager.Instance.GridToWorld(cellPos) + fixedOffset;

            // 堆叠：现有浮字向上移动，为新浮字腾出最底部空间（“往上挤”效果）
            for (int i = 0; i < list.Count; i++)
            {
                list[i].number.transform.DOMoveY(list[i].number.transform.position.y + stackOffsetY, 0.1f)
                    .SetEase(Ease.OutQuad);
            }

            // 从池中获取并初始化新浮字
            FloatingNumber newNumber = GetFromPool();
            newNumber.Initialize(worldPos, value, type);

            // 创建活跃记录并加入列表（新浮字位于最底部，即列表末尾）
            var active = new ActiveFloatingNumber
            {
                number = newNumber,
                type = type,
                currentValue = value,
                remainingMergeTime = mergeWindow
            };
            list.Add(active);
        }
    }

    // ==================== 特殊伤害显示 ====================
    /// <summary>
    /// 显示特殊伤害浮字（向下飘，不参与堆叠和合并）
    /// </summary>
    /// <param name="cellPos">格子坐标</param>
    /// <param name="value">伤害数值</param>
    public void ShowSpecialDamage(Vector2Int cellPos, int value)
    {
        // 计算基础世界坐标
        Vector3 worldPos = GridManager.Instance.GridToWorld(cellPos) + fixedOffset;

        // 从池中获取浮字
        FloatingNumber newNumber = GetFromPool();
        // 特殊初始化（内部会调整初始Y位置更低）
        newNumber.InitializeAsSpecial(worldPos, value);

        // 立即开始飘落动画，完成后直接回收（不存入 activeByCell，不参与任何堆叠/合并）
        newNumber.PlayFloatAway(() =>
        {
            ReturnToPool(newNumber);
        });
    }

    // ==================== 普通浮字的生命周期管理 ====================
    private void Update()
    {
        // 遍历所有格子的活跃浮字，更新合并计时器，超时则触发飘走动画
        List<Vector2Int> cellsToRemove = new List<Vector2Int>();
        foreach (var kvp in activeByCell)
        {
            var cell = kvp.Key;
            var list = kvp.Value;
            // 从后向前遍历，因为可能在回调中删除元素
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var afn = list[i];
                if (afn.remainingMergeTime > 0)
                {
                    afn.remainingMergeTime -= Time.deltaTime;
                    if (afn.remainingMergeTime <= 0)
                    {
                        // 计时归零，开始飘走动画
                        Vector2Int capturedCell = cell;
                        ActiveFloatingNumber capturedAfn = afn;
                        afn.number.PlayFloatAway(() =>
                        {
                            if (activeByCell.TryGetValue(capturedCell, out var list2))
                            {
                                list2.Remove(capturedAfn);
                                if (list2.Count == 0)
                                    activeByCell.Remove(capturedCell);
                            }
                            ReturnToPool(capturedAfn.number);
                            // 重新排列该格子剩余浮字的位置（向下填补空缺）
                            RearrangeStack(capturedCell);
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// 重新排列普通浮字的垂直位置（当某个浮字飘走后，下面的浮字下移填补空缺）
    /// </summary>
    private void RearrangeStack(Vector2Int cell)
    {
        if (!activeByCell.TryGetValue(cell, out var list)) return;
        if (list.Count == 0) return;

        // 按列表顺序重新计算每个浮字的Y偏移：索引0在最底部，偏移0；索引1偏移 stackOffsetY，依此类推
        for (int i = 0; i < list.Count; i++)
        {
            float targetYOffset = i * stackOffsetY;
            Vector3 targetPos = GridManager.Instance.GridToWorld(cell) + fixedOffset;
            targetPos.y += targetYOffset;
            list[i].number.transform.DOMoveY(targetPos.y, 0.1f).SetEase(Ease.OutQuad);
        }
    }
}