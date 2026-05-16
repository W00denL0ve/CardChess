using System.Collections.Generic;

public enum AttributeType
{
    Health,
    MaxHealth,
    Attack,                // 力量（物理伤害基础）
    Intelligence,          // 智慧（法术伤害基础）
    DamageBonus,           // 伤害加成（加算，通用）
    PhysicalDefense,       // 物理护甲
    MagicDefense,          // 魔法抗性（默认0）
    ActionPointLimit,      // 每回合行动力上限
    ActionPoints           // 每回合可用行动次数
}

public class AttributeManager
{
    private Dictionary<AttributeType, Attribute> attributes = new Dictionary<AttributeType, Attribute>();

    /// <summary>
    /// 添加一个属性（如果已存在则设置基础值）
    /// </summary>
    public void AddAttribute(AttributeType type, float baseValue)
    {
        if (attributes.ContainsKey(type))
        {
            attributes[type].baseValue = baseValue;
        }
        else
        {
            attributes[type] = new Attribute(baseValue);
        }
    }

    /// <summary>
    /// 设置属性的基础值
    /// </summary>
    public void SetBaseValue(AttributeType type, float value)
    {
        if (attributes.TryGetValue(type, out var attr))
        {
            attr.baseValue = value;
        }
        Logger.LogWarning($"AttributeManager: 设置属性基础值时找不到属性类型：{type}");
    }

    /// <summary>
    /// 获取属性的基础值
    /// </summary>
    public float GetBaseValue(AttributeType type)
    {
        return attributes.TryGetValue(type, out var attr) ? attr.baseValue : 0f;
    }

    /// <summary>
    /// 获取属性的最终值（含所有修饰器）
    /// </summary>
    public float GetFinalValue(AttributeType type)
    {
        return attributes.TryGetValue(type, out var attr) ? attr.FinalValue : 0f;
    }

    /// <summary>
    /// 检查属性是否存在
    /// </summary>
    public bool HasAttribute(AttributeType type)
    {
        return attributes.ContainsKey(type);
    }

    /// <summary>
    /// 添加修饰器
    /// </summary>
    public void AddModifier(AttributeType type, Modifier modifier)
    {
        if (attributes.TryGetValue(type, out var attr))
        {
            attr.AddModifier(modifier);
            return;
        }
        Logger.LogWarning($"AttributeManager: 添加修饰器时找不到属性类型：{type}");
    }

    /// <summary>
    /// 移除修饰器
    /// </summary>
    public void RemoveModifier(AttributeType type, Modifier modifier)
    {
        if (attributes.TryGetValue(type, out var attr))
        {
            attr.modifiers.Remove(modifier);
            return;
        }
        Logger.LogWarning($"AttributeManager: 移除修饰器时找不到属性类型：{type}");
    }

    /// <summary>
    /// 移除来自指定源的所有修饰器
    /// </summary>
    public void RemoveModifiersFromSource(object source)
    {
        foreach (var attr in attributes.Values)
        {
            attr.RemoveModifiersFromSource(source);
        }
    }
}