using UnityEngine;

public abstract class Buff : ScriptableObject
{
    public string buffId;
    public Sprite icon;
    public string description;

    /// <summary>
    /// <0 永久
    /// </summary>
    public int maxDuration;     
    public bool isDebuff;
    public int maxStack = 1;

    public virtual void OnApply(BuffInstance instance) { }
    public virtual void OnRemove(BuffInstance instance) { }
    public virtual void OnTurnStart(BuffInstance instance) { }
    public virtual void OnTurnEnd(BuffInstance instance) { }
    public virtual void OnUnitMove(BuffInstance instance, Vector2Int from, Vector2Int to) { }
    public virtual void OnBeforeDamageTaken(BuffInstance instance, ref int damage, EffectContext context) { }
    public virtual void OnAfterDamageTaken(BuffInstance instance, int damage, EffectContext context) { }

    protected void AddModifier(BuffInstance instance, float value, ModifierType type, ModifierField field)
    {
        instance.Host.modifierManager.AddModifier(instance.Host, value, ModifierType.Add, ModifierField.None);
    }
    protected void RemoveModifier(BuffInstance instance)
    {
        instance.Host.modifierManager.RemoveModifiersFromSource(instance);
    }
}