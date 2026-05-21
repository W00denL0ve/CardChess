using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Faction { Player, Enemy, Neutral }

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

    // 属性系统
    public AttributeManager AttributeManager { get; private set; }

    // Buff 容器
    public BuffContainer BuffContainer { get; private set; }

    [Header("血条")]
    [SerializeField] private Slider healthBar;

    /// <summary>血量百分比 0~1</summary>
    public float HpPercent => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
    public UnitConfig Config { get; private set; }

    // 网格位置（由 GridManager 设置）
    public Vector2Int GridPosition { get; internal set; }

    // 便捷属性（查询 AttributeManager 的基础值，修饰器仅在具体公式中按需遍历）
    public int CurrentHealth        => (int)AttributeManager.GetFinalValue(AttributeType.Health);
    public int MaxHealth            => (int)AttributeManager.GetFinalValue(AttributeType.MaxHealth);
    public int Attack               => (int)AttributeManager.GetBaseValue(AttributeType.Attack);
    public int Intelligence         => (int)AttributeManager.GetBaseValue(AttributeType.Intelligence);
    public int PhysicalDefense      => (int)AttributeManager.GetBaseValue(AttributeType.PhysicalDefense);
    public int MagicDefense         => (int)AttributeManager.GetBaseValue(AttributeType.MagicDefense);
    public int MovePointLimit     => (int)AttributeManager.GetFinalValue(AttributeType.MovePointLimit);
    public int MovePoints         => (int)AttributeManager.GetFinalValue(AttributeType.MovePoints);
    public int DamageBonus          => (int)AttributeManager.GetBaseValue(AttributeType.DamageBonus);

    // 防御查询
    public int GetDefenseFor(DamageType type) => type == DamageType.Physical ? PhysicalDefense : MagicDefense;

    // 初始化
    public void Initialize(UnitConfig config, Vector2Int gridPos, Faction? overrideFaction = null)
    {
        Config = config;
        unitId = config.unitId;
        occupation = config.occupation;
        this.Faction = overrideFaction ?? config.defaultFaction;
        GridPosition = gridPos;
        IsAlive = true;

        AttributeManager = new AttributeManager();
        foreach (var attr in config.initialAttributes)
            AttributeManager.AddAttribute(attr.type, attr.value);

        BuffContainer = new BuffContainer(this);
        foreach (var buff in config.innateBuffs)
            BuffContainer.ApplyBuff(buff, new EffectContext { executor = new UnitTarget(this) });

        UpdateHealthBar();
    }

    // 伤害（由效果系统调用，finalDamage 已扣除类型防御）
    public void TakeDamage(int finalDamage, EffectContext context = null)
    {
        if (!IsAlive || finalDamage <= 0) return;

        // 使用 BuffContainer 封装的前置回调，允许 Buff 修改伤害值
        BuffContainer.OnBeforeDamageTaken(ref finalDamage, context);

        int oldHealth = CurrentHealth;
        int newHealth = Mathf.Clamp(oldHealth - finalDamage, 0, MaxHealth);
        AttributeManager.SetBaseValue(AttributeType.Health, newHealth);
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, MaxHealth));

        if (newHealth <= 0)
        {
            IsAlive = false;
            GameEventChannel.Dispatch(new UnitDeathEvent(this, context));
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
        int oldHealth = CurrentHealth;
        int newHealth = Mathf.Clamp(oldHealth + amount, 0, MaxHealth);
        AttributeManager.SetBaseValue(AttributeType.Health, newHealth);
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, MaxHealth));
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
            if (snap || path == null || path.Count <= 1)
                yield return appearance.PlayTeleportAnimation(GridToWorld(destination));
            else
                yield return appearance.PlayWalkAnimation(path);
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
        Logger.Log($"Unit: {UnitId}试图获得{amount}点行动力，" + (ignoreLimit ? "忽略" : "约束于") + "行动力上限");
        int limit = (int)AttributeManager.GetBaseValue(AttributeType.MovePointLimit);
        int points = (int)AttributeManager.GetBaseValue(AttributeType.MovePoints) + amount;
        if (points > limit)
        {
            points = limit;
        }
        if (points < 0)
        {
            points = 0;
        }
        AttributeManager.SetBaseValue(AttributeType.MovePoints, points);
        Logger.Log($"Unit: {UnitId}获得{points}点行动力");
        GameEventChannel.Dispatch(new UnitAcquireMovePointEvent(this, points));
    }
}
