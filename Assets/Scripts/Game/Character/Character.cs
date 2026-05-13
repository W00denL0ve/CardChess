using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public Cell currentCell;
    public AttributeManager attributeManager;
    public List<BuffInstance> buffs = new List<BuffInstance>();

    private void Awake()
    {
        attributeManager = GetComponent<AttributeManager>();
    }

    public void MoveTo(Cell targetCell)
    {
        if (currentCell != null)
        {
            // currentCell.ClearOccupyingCharacter();
        }
        // targetCell.SetOccupyingCharacter(this);
        transform.position = targetCell.transform.position;
        // GameEventChannel.Instance.RaiseCharacterMoved(this);
    }

    public void AddBuff(Buff buff, int duration)
    {
        BuffInstance buffInstance = new BuffInstance(buff, duration);
        buffs.Add(buffInstance);
        buff.OnApply(this);
        // GameEventChannel.Instance.RaiseBuffApplied(this, buffInstance);
    }

    public void RemoveBuff(BuffInstance buffInstance)
    {
        buffs.Remove(buffInstance);
        buffInstance.buff.OnRemove(this);
        // GameEventChannel.Instance.RaiseBuffRemoved(this, buffInstance);
    }

    public void TakeDamage(float damage)
    {
        float finalDamage = damage * (1 - attributeManager.GetAttributeValue(AttributeType.DamageReduction));
        // Apply damage
    }

    public void Heal(float amount)
    {
        // Apply heal
    }
}