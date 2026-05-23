using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Faction { Player, Enemy, Neutral }

// 基础属性
[Serializable]
public struct UnitBaseValue
{    
    public int currentHealth;
    public int maxHealth;
    public int attack;
    public int intelligence;
    public int physicalDefense;
    public int magicDefense;
    public int movePointLimit;
    public int movePoints;
    public int hasMoved;
}

public class Unit : MonoBehaviour
{
    // 配置
    [SerializeField] private string unitId;
    [SerializeField] private Occupation occupation;

    // 运行时身份
    public string UnitId => unitId;
    public Occupation Occupation => occupation;
    public Faction Faction { get; private set; }
    public bool IsAlive { get; private set; }

    // 修饰器系统
    public ModifierManager modifierManager = new();

    // 属性
    [SerializeField]
    public UnitBaseValue baseValue;

    // Buff 容器
    public BuffContainer BuffContainer { get; private set; }

    [Header("血条")]
    [SerializeField] private Slider healthBar;

    /// <summary>血量百分比 0~1</summary>
    public float HpPercent => baseValue.maxHealth > 0 ? (float)baseValue.currentHealth / baseValue.maxHealth : 0f;
    public UnitConfig Config { get; private set; }

    // 网格位置（由 GridManager 设置）
    public Vector2Int GridPosition { get; internal set; }

    // 防御查询
    public int GetDefenseFor(DamageType type) => type == DamageType.Physical ? baseValue.physicalDefense : baseValue.magicDefense;

    private void Start()
    {
        GameEventChannel.Register<TurnStartedEvent>(OnTurnStarted);
    }

    public void OnTurnStarted(TurnStartedEvent evt)
    {
        baseValue.hasMoved = 0;
    }


    // 初始化
    public void Initialize(UnitConfig config, Vector2Int gridPos, Faction? overrideFaction = null)
    {
        Config = config;
        unitId = config.unitId;
        occupation = config.occupation;
        Faction = overrideFaction ?? config.defaultFaction;
        GridPosition = gridPos;
        IsAlive = true;
        baseValue = config.initialValue;
        BuffContainer = new BuffContainer(this);
        foreach (var buff in config.innateBuffs)
            BuffContainer.ApplyBuff(buff, new UnitTarget(this));

        UpdateHealthBar();
    }

    // 伤害（由效果系统调用，finalDamage 已扣除类型防御）
    public void TakeDamage(int finalDamage, EffectContext context = null)
    {
        if (!IsAlive || finalDamage <= 0) return;

        // 使用 BuffContainer 封装的前置回调，允许 Buff 修改伤害值
        BuffContainer.OnBeforeDamageTaken(ref finalDamage, context);

        int oldHealth = baseValue.currentHealth;
        int newHealth = Mathf.Clamp(oldHealth - finalDamage, 0, baseValue.maxHealth);
        baseValue.currentHealth = newHealth;
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, baseValue.maxHealth));

        if (newHealth <= 0)
        {
            IsAlive = false;
            LevelManager.Instance.HandleUnitDeath(this, context);
        }
        else
        {
            // 使用 BuffContainer 封装的后置回调
            BuffContainer.OnAfterDamageTaken(finalDamage, context);
        }

        UpdateHealthBar();
    }

    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="context"></param>
    public void Heal(int amount, EffectContext context = null)
    {
        if (!IsAlive || amount <= 0) return;
        int oldHealth = baseValue.currentHealth;
        int newHealth = Mathf.Clamp(oldHealth + amount, 0, baseValue.maxHealth);
        baseValue.currentHealth = newHealth;
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, baseValue.maxHealth));
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = HpPercent;
    }


    /// <summary>
    /// 异步移动到目标格子 — 表现层逐格走路，数据层瞬移
    /// 完成后自动派发 UnitMovedEvent，GridManager 响应更新格子占用
    /// </summary>
    /// <param name="destination">目标格子坐标</param>
    /// <param name="path">完整路径（包含起点和终点），用于视觉动画</param>
    /// <param name="snap">true=瞬移（播放瞬移动画），false=逐格走路</param>
    public IEnumerator MoveTo(Vector2Int destination, List<Vector2Int> path, bool snap = false)
    {
        if (!IsAlive) yield break;

        // 表现层：播放移动动画
        var appearance = GetComponent<UnitAppearance>();
        if (appearance != null)
        {
            if (snap || path == null)
                yield return appearance.PlayTeleportAnimation(GridToWorld(destination));
            else
                yield return appearance.PlayWalkAnimation(path);
        }

        // 设置本回合已行动步数
        if (path != null)
        {
            baseValue.hasMoved += Mathf.Clamp(path.Count - 1, 0, int.MaxValue);
        }

        // 数据层：瞬移
        Vector2Int from = GridPosition;
        GridPosition = destination;
        // 移动后刷新 Y 轴排序
        if (appearance != null) appearance.RefreshSortingOrder();
        GameEventChannel.Dispatch(new UnitMovedEvent(this, from, destination));
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return UnitFactory.GetWorldPosition(gridPos, Config);
    }

    /// <summary>
    /// 获得行动力
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="ignoreLimit"></param> 
    /// <param name="context"></param>
    public void AcquireMovePoint(int amount, bool ignoreLimit = false, EffectContext context = null)
    {
        if (!IsAlive) return;
        //Logger.Log($"Unit: {UnitId}试图获得{amount}点行动力，" + (ignoreLimit ? "忽略" : "约束于") + "行动力上限");
        int limit = baseValue.movePointLimit;
        int points = Mathf.Clamp(baseValue.movePoints + amount, 0, limit);
        baseValue.movePoints = points;
        //Logger.Log($"Unit: {UnitId}获得{points}点行动力");
        GameEventChannel.Dispatch(new UnitAcquireMovePointEvent(this, points));
    }
}
