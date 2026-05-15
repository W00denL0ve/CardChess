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
    public Faction faction { get; private set; }
    public bool isAlive { get; private set; }

    // 属性系统
    public AttributeManager attributeManager { get; private set; }

    // Buff 容器
    public BuffContainer buffContainer { get; private set; }

    // 网格位置（由 GridManager 设置）
    public Vector2Int gridPosition { get; internal set; }

    // 便捷属性（查询 AttributeManager 的最终值）
    public int currentHealth        => (int)attributeManager.GetFinalValue(AttributeType.Health);
    public int maxHealth            => (int)attributeManager.GetFinalValue(AttributeType.MaxHealth);
    public int attack               => (int)attributeManager.GetFinalValue(AttributeType.Attack);
    public int intelligence         => (int)attributeManager.GetFinalValue(AttributeType.Intelligence);
    public int physicalDefense      => (int)attributeManager.GetFinalValue(AttributeType.PhysicalDefense);
    public int magicDefense         => (int)attributeManager.GetFinalValue(AttributeType.MagicDefense);
    public int actionPointLimit     => (int)attributeManager.GetFinalValue(AttributeType.ActionPointLimit);
    public int actionPoints         => (int)attributeManager.GetFinalValue(AttributeType.ActionPoints);
    public int damageBonus          => (int)attributeManager.GetFinalValue(AttributeType.DamageBonus);

    // 防御查询
    public int GetDefenseFor(DamageType type) => type == DamageType.Physical ? physicalDefense : magicDefense;

    // 初始化
    public void Initialize(UnitConfig config, Faction faction, Vector2Int gridPos)
    {
        unitId = config.unitId;
        occupation = config.occupation;
        this.faction = faction;
        gridPosition = gridPos;
        isAlive = true;

        attributeManager = new AttributeManager();
        foreach (var attr in config.initialAttributes)
            attributeManager.AddAttribute(attr.type, attr.value);

        buffContainer = new BuffContainer(this);
        foreach (var buff in config.innateBuffs)
            buffContainer.ApplyBuff(buff, new EffectContext { caster = gameObject });
    }

    // 伤害（由效果系统调用，finalDamage 已扣除类型防御）
    public void TakeDamage(int finalDamage, EffectContext context = default)
    {
        if (!isAlive || finalDamage <= 0) return;

        // 使用 BuffContainer 封装的前置回调，允许 Buff 修改伤害值
        buffContainer.OnBeforeDamageTaken(ref finalDamage, context);

        int oldHealth = currentHealth;
        int newHealth = Mathf.Clamp(oldHealth - finalDamage, 0, maxHealth);
        attributeManager.SetBaseValue(AttributeType.Health, newHealth);
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, maxHealth));

        if (newHealth <= 0)
        {
            isAlive = false;
            GameEventChannel.Dispatch(new UnitDeathEvent(this, context));
        }
        else
        {
            // 使用 BuffContainer 封装的后置回调
            buffContainer.OnAfterDamageTaken(finalDamage, context);
        }
    }

    // 治疗
    public void Heal(int amount, EffectContext context = default)
    {
        if (!isAlive || amount <= 0) return;
        int oldHealth = currentHealth;
        int newHealth = Mathf.Clamp(oldHealth + amount, 0, maxHealth);
        attributeManager.SetBaseValue(AttributeType.Health, newHealth);
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, maxHealth));
    }

    // 移动请求（由效果系统调用）
    public void RequestMove(Vector2Int targetPos, EffectContext context = default)
    {
        if (!isAlive) return;
        GameEventChannel.Dispatch(new UnitMoveRequestEvent(this, gridPosition, targetPos, context));
    }
}
