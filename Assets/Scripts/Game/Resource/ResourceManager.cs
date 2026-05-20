using UnityEngine;

/// <summary>
/// 玩家资源管理器 — 统一管理玩家所有资源（标量 + 集合）
/// 加载 / 存档由外部通过 LoadFromRunState / SaveToRunState 触发
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

        [Header("初始值")]
    [SerializeField] private int initialEnergy = 6;
    [SerializeField] private int initialMaxEnergy = 6;
    [SerializeField] private int initialGold = 0;

    // ── 运行时资源（public get，外部只读） ──
    public int Energy    { get; private set; }
    public int MaxEnergy { get; private set; }
    public int Gold      { get; private set; }

    // ── 集合资源 ──
    /// <summary>牌库中卡牌的 Addressable 地址列表（运行时直接引用 RunState，不额外复制）</summary>
    public System.Collections.Generic.List<string> DeckCardIds { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ═══ 加载 / 存档 ═══

    /// <summary>从 RunState 加载资源（新局 / 读档时调用）</summary>
    public void LoadFromRunState()
    {
        var run = SaveManager.Instance?.CurrentRun;
        if (run == null)
        {
            // 无存档时使用默认值
            Energy    = initialEnergy;
            MaxEnergy = initialMaxEnergy;
            Gold      = initialGold;
            DeckCardIds = new System.Collections.Generic.List<string>();
            return;
        }

        // 持久化的量
        MaxEnergy = run.maxEnergy;
        Gold      = run.gold;
        DeckCardIds = run.deckCardIds ?? new System.Collections.Generic.List<string>();

        // 非持久化的量：每局从上限重置
        Energy = MaxEnergy;
    }

    /// <summary>将运行时资源写回 RunState（存档时调用）</summary>
    public void SaveToRunState()
    {
        var run = SaveManager.Instance?.CurrentRun;
        if (run == null) return;

        run.maxEnergy   = MaxEnergy;
        run.gold        = Gold;
        run.deckCardIds = DeckCardIds;
        // Energy 不存——每局开始从 MaxEnergy 重置
    }

    // ═══ 标量操作 ═══

    /// <summary>消耗能量，返回是否成功</summary>
    public bool SpendEnergy(int cost)
    {
        if (cost < 0 || Energy < cost) return false;
        SetAndNotify(ResourceType.Energy, Energy - cost);
        return true;
    }

    /// <summary>增加能量（不超过上限）</summary>
    public void GainEnergy(int amount)
    {
        if (amount < 0) return;
        SetAndNotify(ResourceType.Energy, Mathf.Min(Energy + amount, MaxEnergy));
    }
    
    /// <summary>每回合重置能量（回满 + 偏移量），强制派发事件确保 UI 同步</summary>
    public void RefreshEnergy(int offset)
    {
        int oldValue = Energy;
        Energy = Mathf.Clamp(MaxEnergy + offset, 0, MaxEnergy * 2);
        GameEventChannel.Dispatch(new ResourceChangedEvent
        {
            type = ResourceType.Energy,
            oldValue = oldValue,
            newValue = Energy
        });
    }

    /// <summary>增加金币</summary>
    public void GainGold(int amount)
    {
        if (amount < 0) return;
        SetAndNotify(ResourceType.Gold, Gold + amount);
    }

    /// <summary>消耗金币，返回是否成功</summary>
    public bool SpendGold(int amount)
    {
        if (amount < 0 || Gold < amount) return false;
        SetAndNotify(ResourceType.Gold, Gold - amount);
        return true;
    }

    /// <summary>设置能量上限（同时钳制当前能量）</summary>
    public void SetMaxEnergy(int value)
    {
        if (value < 0) value = 0;
        int oldMax = MaxEnergy;
        MaxEnergy = value;
        Energy = Mathf.Min(Energy, MaxEnergy);
        if (MaxEnergy != oldMax)
            GameEventChannel.Dispatch(new ResourceChangedEvent
            {
                type = ResourceType.MaxEnergy,
                oldValue = oldMax,
                newValue = MaxEnergy
            });
    }

    // ═══ 内部 ═══

    private void SetAndNotify(ResourceType type, int newValue)
    {
        int oldValue = GetValue(type);
        if (oldValue == newValue) return;
        SetValue(type, newValue);
        GameEventChannel.Dispatch(new ResourceChangedEvent
        {
            type = type,
            oldValue = oldValue,
            newValue = newValue
        });
    }

    private int GetValue(ResourceType type) => type switch
    {
        ResourceType.Energy    => Energy,
        ResourceType.MaxEnergy => MaxEnergy,
        ResourceType.Gold      => Gold,
        _ => 0
    };

    private void SetValue(ResourceType type, int value)
    {
        switch (type)
        {
            case ResourceType.Energy:    Energy = value;    break;
            case ResourceType.MaxEnergy: MaxEnergy = value; break;
            case ResourceType.Gold:      Gold = value;      break;
        }
    }
}
