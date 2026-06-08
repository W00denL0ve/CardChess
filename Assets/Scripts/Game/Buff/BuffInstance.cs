using System.Collections.Generic;

public class BuffInstance
{
    public Buff BuffData { get; }
    public Unit Host { get; }
    public ITarget Caster { get; }

    public int RemainingDuration { get; set; }
    public int CurrentStacks { get; private set; }

    /// <summary>是否已过期（Tick 后更新，Container 读取决定是否移除）</summary>
    public bool IsExpired { get; private set; }

    /// <summary>请求移除的回调（由 BuffContainer 在创建时注入）</summary>
    internal System.Action RequestRemove;

    // 显式追踪本实例添加的修饰器（与 ModifierManager 持有同一引用）
    private List<Modifier> trackedModifiers = new();

    public BuffInstance(Buff data, Unit host, ITarget caster, int duration)
    {
        BuffData = data;
        Host = host;
        RemainingDuration = duration;
        CurrentStacks = 1;
        Caster = caster;
    }

    /// <summary>向宿主添加修饰器并自动追踪引用</summary>
    public Modifier AddModifier(float value, ModifierType type, ModifierField field)
    {
        var mod = new Modifier(this, value, type, field);
        Host.modifierManager.AddModifier(mod);
        trackedModifiers.Add(mod);
        return mod;
    }

    /// <summary>尝试增加一层堆叠</summary>
    public bool AddStack()
    {
        if (CurrentStacks >= BuffData.maxStack)
            return false;
        CurrentStacks++;
        if (BuffData is IOnApplyBuff onApply)
            onApply.OnApply(this);
        return true;
    }

    /// <summary> 尝试增加多重堆叠 </summary>
    public bool AddStack(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (!AddStack())
                return false;
        }
        return true;
    }

    /// <summary>减少一层堆叠，返回是否完全移除</summary>
    public bool RemoveStack()
    {
        if (CurrentStacks <= 0) return false;
        CurrentStacks--;
        if (CurrentStacks == 0)
        {
            Cleanup();
            return true;
        }
        return false;
    }

    /// <summary>移除指定数量的堆叠</summary>
    public void RemoveStack(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if(RemoveStack())
            {
                Expire();
                return;
            }
        }
    }
    

    /// <summary>每回合结束时调用：减少持续时间，标记过期</summary>
    public void Tick()
    {
        if (BuffData.defaultDuration < 0) return; // 永久
        RemainingDuration--;
        if (RemainingDuration <= 0)
        {
            Expire();
        }
    }

    /// <summary>主动标记过期并请求移除（供 Buff 在中间时刻调用，如护盾被打破）</summary>
    public void Expire()
    {
        IsExpired = true;
        RequestRemove?.Invoke();
    }

    /// <summary>清理该实例添加的所有修饰器</summary>
    public void Cleanup()
    {
        Host.modifierManager.RemoveModifiersFromSource(this);
        trackedModifiers.Clear();
    }
}