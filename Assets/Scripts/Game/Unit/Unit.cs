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
    public int ActionPointLimit     => (int)AttributeManager.GetFinalValue(AttributeType.ActionPointLimit);
    public int ActionPoints         => (int)AttributeManager.GetFinalValue(AttributeType.ActionPoints);
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
    public void TakeDamage(int finalDamage, EffectContext context = default)
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

    // 治疗
    public void Heal(int amount, EffectContext context = default)
    {
        if (!IsAlive || amount <= 0) return;
        int oldHealth = CurrentHealth;
        int newHealth = Mathf.Clamp(oldHealth + amount, 0, MaxHealth);
        AttributeManager.SetBaseValue(AttributeType.Health, newHealth);
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, MaxHealth));
    }

    // 移动请求（由效果系统调用）
    public void RequestMove(Vector2Int targetPos, EffectContext context = default)
    {
        if (!IsAlive) return;
        GameEventChannel.Dispatch(new UnitMoveRequestEvent(this, GridPosition, targetPos, context));
    }
}
