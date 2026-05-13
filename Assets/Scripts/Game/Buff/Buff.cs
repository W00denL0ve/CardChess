using UnityEngine;

public abstract class Buff : ScriptableObject
{
    public string buffName;
    public Sprite icon;
    public string description;

    public virtual void OnApply(Character target) { }
    public virtual void OnRemove(Character target) { }
    public virtual void OnTurnStart(Character target) { }
    public virtual void OnTurnEnd(Character target) { }
    public virtual void OnMove(Character target) { }
    public virtual void OnBeforeDamageTaken(Character target, ref float damage) { }
}