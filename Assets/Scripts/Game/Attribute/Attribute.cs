using System.Collections.Generic;

public class Attribute
{
    public float baseValue;
    public List<Modifier> modifiers = new List<Modifier>();

    public Attribute(float baseValue)
    {
        this.baseValue = baseValue;
    }

    public float FinalValue
    {
        get
        {
            float addSum = 0;
            float multiplySum = 1;
            float finalAddSum = 0;
            float finalMultiplySum = 1;

            foreach (var mod in modifiers)
            {
                switch (mod.type)
                {
                    case ModifierType.Add:
                        addSum += mod.value;
                        break;
                    case ModifierType.Multiply:
                        multiplySum *= mod.value;
                        break;
                    case ModifierType.FinalAdd:
                        finalAddSum += mod.value;
                        break;
                    case ModifierType.FinalMultiply:
                        finalMultiplySum *= mod.value;
                        break;
                }
            }

            return ((baseValue + addSum) * multiplySum + finalAddSum) * finalMultiplySum;
        }
    }

    public void AddModifier(Modifier modifier)
    {
        modifiers.Add(modifier);
    }

    public void RemoveModifiersFromSource(object source)
    {
        modifiers.RemoveAll(m => m.source == source);
    }
}