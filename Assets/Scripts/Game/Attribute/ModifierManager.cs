using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 修饰器管理器，每个可以挂修饰器的对象一个
/// </summary>
public class ModifierManager
{
    private List<Modifier> modifiers = new();

    /// <summary>
    /// 添加修饰器
    /// </summary>
    /// <param name="source">来源，调用者自行处理</param>
    /// <param name="value">修饰器值</param>
    /// <param name="type">修饰器类型</param>
    /// <param name="field">修饰器作用域</param>
    public void AddModifier(object source, float value, ModifierType type, ModifierField field)
    {
        modifiers.Add(new Modifier(source, value, type, field));
    }

    /// <summary>
    /// 添加修饰器
    /// </summary>
    public void AddModifier(Modifier modifier)
    {
        modifiers.Add(modifier);
    }

    /// <summary>
    /// 移除来自指定源的所有修饰器
    /// </summary>
    public void RemoveModifiersFromSource(object source)
    {
        modifiers.RemoveAll(modifier => modifier.source == source);
    }

    /// <summary>
    /// 根据给定修饰域获取修饰器
    /// </summary>
    /// <param name="field">修饰域</param>
    /// <returns>修饰器列表</returns>
    public List<Modifier> GetModifiers(ModifierField field)
    {
        List<Modifier> mods = new();
        if (modifiers == null)
            return mods;
        foreach (Modifier modifier in modifiers)
        {
            if (modifier.field.HasFlag(field))
            {
                mods.Add(modifier);
            }
        }
        return mods;
    }

    /// <summary>
    /// 根据给定修饰器类型获取修饰器
    /// </summary>
    /// <param name="type">修饰器类型</param>
    /// <returns>修饰器列表</returns>
    public List<Modifier> GetModifiers(ModifierType type)
    {
        List<Modifier> mods = new();
        if (modifiers == null)
            return mods;
        foreach (Modifier modifier in modifiers)
        {
            if (modifier.type == type)
            {
                mods.Add(modifier);
            }
        }
        return mods;
    }
}