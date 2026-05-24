using System;
/// <summary>
/// 修饰器类型，决定在哪里计算
/// </summary>
public enum ModifierType
{
    Add,
    Multiply,
    FinalAdd,
    FinalMultiply
}

/// <summary>
/// 修饰器作用域，决定作用于哪些属性
/// </summary>
[Flags]
public enum ModifierField
{
    None = 0,
    PhysicalDefense = 1 << 0,       // 物理护甲
    MagicDefense = 1 << 1,          // 魔法抗性
    Physic = 1 << 2,          // 物理攻击
    Magic = 1 << 3,           // 魔法攻击
    BackAttack = 1 << 4,      // 背刺
    Heal = 1 << 5,            // 治疗量
    All = 1 << 6 - 1
}

[System.Serializable]
public class Modifier
{
    public object source;
    public float value;
    public ModifierType type;

    public ModifierField field;

    public Modifier(object source, float value, ModifierType type, ModifierField field)
    {
        this.source = source;
        this.value = value;
        this.type = type;
        this.field = field;
    }
}

