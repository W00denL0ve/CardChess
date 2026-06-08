using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Buff 容器 —— 每个 Unit 一个，管理 Buff 实例生命周期。
/// 通过事件驱动：订阅 Unit 本地事件，按接口将事件转发给所有 Buff。
/// </summary>
public class BuffContainer
{
    private Unit host;
    private List<BuffInstance> buffs = new();

    public BuffContainer(Unit host)
    {
        this.host = host;

        // 订阅 Unit 本地事件（实例作用域，自动过滤）
        host.BeforeDamage += OnBeforeDamage;
        host.AfterDamage  += OnAfterDamage;
        host.Moved        += OnUnitMoved;
    }

    /// <summary>清理订阅（Unit 销毁时调用）</summary>
    public void Dispose()
    {
        host.BeforeDamage -= OnBeforeDamage;
        host.AfterDamage  -= OnAfterDamage;
        host.Moved        -= OnUnitMoved;
    }

    // ========== 事件转发 ==========

    private void OnBeforeDamage(DamageModifyEvent evt)
    {
        int damage = evt.Damage;
        foreach (var b in buffs.ToList())
        {
            if (b.BuffData is IOnBeforeDmg beforeDmg)
                beforeDmg.OnBeforeDamageTaken(b, ref damage, evt.Context);
        }
        evt.Damage = damage;
    }

    private void OnAfterDamage(DamageModifyEvent evt)
    {
        foreach (var b in buffs.ToList())
        {
            if (b.BuffData is IOnAfterDmg afterDmg)
                afterDmg.OnAfterDamageTaken(b, evt.Damage, evt.Context);
        }
    }

    private void OnUnitMoved(Vector2Int from, Vector2Int to)
    {
        foreach (var b in buffs.ToList())
        {
            if (b.BuffData is IOnMoveBuff onMove)
                onMove.OnMove(b, from, to);
        }
    }

    /// <summary>攻击方位修正（由 Unit.GetAttackPositionFromTarget 调用）</summary>
    public void ModifyAttackPosition(AttackPosEvent evt)
    {
        foreach (var b in buffs.ToList())
            if (b.BuffData is IAttackPositionModifier mod)
                evt.Position = mod.ModifyAttackPosition(evt.Position);
    }

    /// <summary>受击方位修正（由 Unit.GetAttackPositionFromTarget 调用）</summary>
    public void ModifyHitPosition(AttackPosEvent evt)
    {
        foreach (var b in buffs.ToList())
            if (b.BuffData is IAttackPositionModifier mod)
                evt.Position = mod.ModifyHitPosition(evt.Position);
    }

    // ========== 回合流程 ==========

    /// <summary>由 Unit.OnTurnStarted 调用</summary>
    public void OnTurnStarted()
    {
        foreach (var b in buffs)
        {
            if (b.BuffData is IOnTurnStart turnStart)
                turnStart.OnTurnStart(b);
        }
    }

    /// <summary>回合结束：Tick 所有 Buff，过期自动移除</summary>
    public void OnTurnEnd()
    {
        foreach (var b in buffs.ToList())
        {
            if (b.BuffData is IOnTurnEnd turnEnd)
                turnEnd.OnTurnEnd(b);
            b.Tick(); // 内部标记过期并调用 RequestRemove
        }
    }

    // ========== Buff 管理 ==========

        public void ApplyBuff(Buff buffData, ITarget caster, int? overrideDuration = null)
    {
        var existing = buffs.FirstOrDefault(b => b.BuffData == buffData);

        switch (buffData.stackStrategy)
        {
            case BuffStackStrategy.Refresh:
                if (existing != null)
                {
                    if (existing.AddStack()) return;
                    existing.RemainingDuration = overrideDuration ?? buffData.defaultDuration;
                    return;
                }
                break;

            case BuffStackStrategy.Overwrite:
                if (existing != null) RemoveInstance(existing);
                break;

            case BuffStackStrategy.Separate:
                // 不查重，直接创建
                break;
        }

        var instance = new BuffInstance(buffData, host, caster,
            overrideDuration ?? buffData.defaultDuration);
        instance.RequestRemove = () => RemoveInstance(instance);
        buffs.Add(instance);
        if (buffData is IOnApplyBuff onApply)
            onApply.OnApply(instance);
        GameEventChannel.Dispatch(new BuffAppliedEvent(host, instance));
    }

    public void RemoveBuff(Buff buffData)
    {
        var instance = buffs.FirstOrDefault(b => b.BuffData == buffData);
        if (instance != null)
            RemoveInstance(instance);
    }

    private void RemoveInstance(BuffInstance instance)
    {
        if (!buffs.Remove(instance)) return;
        if (instance.BuffData is IOnRemoveBuff onRemove)
            onRemove.OnRemove(instance);
        else
            instance.Cleanup();
        GameEventChannel.Dispatch(new BuffRemovedEvent(host, instance.BuffData));
    }

    public List<BuffInstance> GetAllBuffs() => buffs;
}
