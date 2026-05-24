using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffContainer
{
    private Unit host;
    private List<BuffInstance> buffs = new();

    public BuffContainer(Unit unit) => host = unit;

    /// <summary>应用一个 Buff</summary>
    public void ApplyBuff(Buff buffData, ITarget caster)
    {
        if (buffData == null) return;
        var existing = buffs.FirstOrDefault(b => b.BuffData == buffData);
        if (existing != null)
        {
            if (existing.AddStack()) return;
            existing.RemainingDuration = buffData.maxDuration;
            return;
        }
        var instance = new BuffInstance(buffData, host, caster);
        buffs.Add(instance);
        buffData.OnApply(instance);
    }

    /// <summary>手动移除一个 Buff</summary>
    public void RemoveBuff(Buff buffData)
    {
        var instance = buffs.FirstOrDefault(b => b.BuffData == buffData);
        if (instance == null) return;
        instance.Cleanup();
        buffData.OnRemove(instance);
        buffs.Remove(instance);
    }

    /// <summary>获取所有 Buff 实例</summary>
    public List<BuffInstance> GetAllBuffs() => buffs;

    // ========== 新增：生命周期转发方法 ==========

    /// <summary>伤害前回调（允许 Buff 修改伤害值）</summary>
    public void OnBeforeDamageTaken(ref int damage, EffectContext context)
    {
        foreach (var b in buffs.ToList())
            b.BuffData.OnBeforeDamageTaken(b, ref damage, context);
    }

    /// <summary>伤害后回调</summary>
    public void OnAfterDamageTaken(int damage, EffectContext context)
    {
        foreach (var b in buffs.ToList())
            b.BuffData.OnAfterDamageTaken(b, damage, context);
    }

    /// <summary>移动后回调</summary>
    public void OnUnitMove(Vector2Int from, Vector2Int to)
    {
        foreach (var b in buffs.ToList())
            b.BuffData.OnUnitMove(b, from, to);
    }

    /// <summary>作为攻击者判断攻击方位前回调</summary>
    public AttackPosition OnBeforeAttackPosition(AttackPosition position)
    {
        foreach (var b in buffs.ToList())
        if (b.BuffData is IAttackPositionModifier mod)
            position = mod.ModifyAttackPosition(position);
        return position;
    }

    /// <summary>作为受击者判断受击方位前回调</summary>
    public AttackPosition OnBeforeHitPosition(AttackPosition position)
    {
        foreach (var b in buffs.ToList())
        if (b.BuffData is IAttackPositionModifier mod)
            position = mod.ModifyHitPosition(position);
        return position;
    }

    // ========== 原有：回合流程 ==========

    public void OnTurnStart()
    {
        foreach (var b in buffs.ToList())
            b.BuffData.OnTurnStart(b);
    }

    public void OnTurnEnd()
    {
        var expired = new List<BuffInstance>();
        foreach (var b in buffs.ToList())
        {
            b.BuffData.OnTurnEnd(b);
            if (b.TickDuration())
                expired.Add(b);
        }
        foreach (var b in expired)
        {
            b.Cleanup();
            b.BuffData.OnRemove(b);
            buffs.Remove(b);
        }
    }
}
