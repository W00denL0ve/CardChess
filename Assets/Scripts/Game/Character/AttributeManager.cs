using System.Collections.Generic;
using UnityEngine;

public enum AttributeType
{
    Attack,
    DamageBonus,
    DamageReduction,
    Health,
    MaxHealth
}

public class AttributeManager : MonoBehaviour
{
    private Dictionary<AttributeType, Attribute> attributes = new Dictionary<AttributeType, Attribute>();

    private void Awake()
    {
        // Initialize default attributes
        attributes[AttributeType.Attack] = new Attribute(10f);
        attributes[AttributeType.DamageBonus] = new Attribute(0f);
        attributes[AttributeType.DamageReduction] = new Attribute(0f);
        attributes[AttributeType.Health] = new Attribute(100f);
        attributes[AttributeType.MaxHealth] = new Attribute(100f);
    }

    public float GetAttributeValue(AttributeType type)
    {
        return attributes[type].FinalValue;
    }

    public void AddModifier(AttributeType type, Modifier modifier)
    {
        attributes[type].AddModifier(modifier);
    }

    public void RemoveModifiersFromSource(object source)
    {
        foreach (var attr in attributes.Values)
        {
            attr.RemoveModifiersFromSource(source);
        }
    }
}