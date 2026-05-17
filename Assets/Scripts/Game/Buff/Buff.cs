using UnityEngine;

public abstract class Buff : ScriptableObject
{
    public string buffId;
    public string buffName;
    public Sprite icon;
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

    protected void AddModifier(BuffInstance instance, AttributeType type, Modifier modifier)
    {
        instance.Host.AttributeManager.AddModifier(type, modifier);
        instance.RegisterModifier(type, modifier);
    }
    protected void RemoveModifier(BuffInstance instance, AttributeType type, Modifier modifier)
    {
        instance.Host.AttributeManager.RemoveModifier(type, modifier);
        instance.UnregisterModifier(type, modifier);
    }
}