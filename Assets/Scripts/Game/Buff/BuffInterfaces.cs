using UnityEngine;

// ========================================
//  Buff 生命周期接口 — 按需实现，不强制
// ========================================

/// <summary>Buff 被施加时</summary>
public interface IOnApplyBuff
{
    void OnApply(BuffInstance instance);
}

/// <summary>Buff 被移除时</summary>
public interface IOnRemoveBuff
{
    void OnRemove(BuffInstance instance);
}

/// <summary>每回合开始时</summary>
public interface IOnTurnStart
{
    void OnTurnStart(BuffInstance instance);
}

/// <summary>每回合结束时</summary>
public interface IOnTurnEnd
{
    void OnTurnEnd(BuffInstance instance);
}

/// <summary>获取移动点数时</summary>
public interface IOnGetMovePoint
{
    void OnGetMovePoint(BuffInstance instance, ref int movePoint);
}

/// <summary>单位移动后</summary>
public interface IOnMoveBuff
{
    void OnMove(BuffInstance instance, Vector2Int from, Vector2Int to);
}

/// <summary>受到伤害前（可修改伤害值）</summary>
public interface IOnBeforeDmg
{
    void OnBeforeDamageTaken(BuffInstance instance, ref int damage, EffectContext context);
}

/// <summary>受到伤害后</summary>
public interface IOnAfterDmg
{
    void OnAfterDamageTaken(BuffInstance instance, int damage, EffectContext context);
}
