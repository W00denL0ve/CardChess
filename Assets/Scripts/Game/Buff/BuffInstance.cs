using System.Collections.Generic;

public class BuffInstance
{
    public Buff BuffData { get; }
    public Unit Host { get; }
    public EffectContext SourceContext { get; }

    public int RemainingDuration { get; set; }
    public int CurrentStacks { get; private set; }

    private List<(AttributeType type, Modifier modifier)> appliedModifiers = new();

    public BuffInstance(Buff data, Unit host, EffectContext source = default)
    {
        BuffData = data;
        Host = host;
        RemainingDuration = data.maxDuration;
        CurrentStacks = 1;
        SourceContext = source;
    }

    /// <summary>记录一个修饰器</summary>
    public void RegisterModifier(AttributeType type, Modifier modifier)
    {
        appliedModifiers.Add((type, modifier));
    }

    /// <summary>移除一个修饰器的记录</summary>
    public void UnregisterModifier(AttributeType type, Modifier modifier)
    {
        appliedModifiers.Remove((type, modifier));
    }

    /// <summary>
    /// 查找匹配的修饰器（根据属性类型、修饰器类型和来源）
    /// </summary>
    /// <param name="type">属性类型</param>
    /// <param name="modifierType">修饰器类型（Add/Multiply/FinalAdd/FinalMultiply）</param>
    /// <param name="source">修饰器来源（通常是 Buff 资产本身）</param>
    /// <returns>找到的修饰器，不存在则返回 null</returns>
    public Modifier FindModifier(AttributeType type, ModifierType modifierType, object source)
    {
        foreach (var item in appliedModifiers)
        {
            if (item.type == type && item.modifier.type == modifierType && item.modifier.source == source)
                return item.modifier;
        }
        return null;
    }

    /// <summary>尝试增加一层堆叠</summary>
    public bool AddStack()
    {
        if (CurrentStacks >= BuffData.maxStack)
            return false;
        CurrentStacks++;
        BuffData.OnApply(this);
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

    /// <summary>回合倒计时，返回是否过期</summary>
    public bool TickDuration()
    {
        if (BuffData.maxDuration < 0) return false;
        RemainingDuration--;
        return RemainingDuration <= 0;
    }

    /// <summary>清理该实例添加的所有修饰器</summary>
    public void Cleanup()
    {
        foreach (var (type, modifier) in appliedModifiers)
        {
            Host.AttributeManager.RemoveModifier(type, modifier);
        }
        appliedModifiers.Clear();
    }
}