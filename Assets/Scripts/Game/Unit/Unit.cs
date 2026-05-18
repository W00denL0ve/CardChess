using UnityEngine;

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

    // 网格位置（由 GridManager 设置）
    public Vector2Int GridPosition { get; internal set; }

    // 便捷属性（查询 AttributeManager 的最终值）
    public int CurrentHealth        => (int)AttributeManager.GetFinalValue(AttributeType.Health);
    public int MaxHealth            => (int)AttributeManager.GetFinalValue(AttributeType.MaxHealth);
    public int Attack               => (int)AttributeManager.GetFinalValue(AttributeType.Attack);
    public int Intelligence         => (int)AttributeManager.GetFinalValue(AttributeType.Intelligence);
    public int PhysicalDefense      => (int)AttributeManager.GetFinalValue(AttributeType.PhysicalDefense);
    public int MagicDefense         => (int)AttributeManager.GetFinalValue(AttributeType.MagicDefense);
    public int MovePointLimit     => (int)AttributeManager.GetFinalValue(AttributeType.MovePointLimit);
    public int MovePoints         => (int)AttributeManager.GetFinalValue(AttributeType.MovePoints);
    public int DamageBonus          => (int)AttributeManager.GetFinalValue(AttributeType.DamageBonus);

    // 防御查询
    public int GetDefenseFor(DamageType type) => type == DamageType.Physical ? PhysicalDefense : MagicDefense;

    // 初始化
    public void Initialize(UnitConfig config, Faction faction, Vector2Int gridPos)
    {
        unitId = config.unitId;
        occupation = config.occupation;
        this.Faction = faction;
        GridPosition = gridPos;
        IsAlive = true;

        AttributeManager = new AttributeManager();
        foreach (var attr in config.initialAttributes)
            AttributeManager.AddAttribute(attr.type, attr.value);

        BuffContainer = new BuffContainer(this);
        foreach (var buff in config.innateBuffs)
            BuffContainer.ApplyBuff(buff, new EffectContext { executor = new UnitTarget(this) });
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
    }


    /// <summary>
    /// 移动请求（效果系统调用）
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="context"></param>
    public void RequestMove(Vector2Int targetPos, EffectContext context = null, bool clearPoints = true)
    {
        if (!IsAlive) return;
        GameEventChannel.Dispatch(new UnitMoveRequestEvent(this, GridPosition, targetPos, context));
        if (clearPoints) // 移动后行动力默认清零不保留
        {
            AttributeManager.SetBaseValue(AttributeType.MovePoints, 0); 
        }
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
