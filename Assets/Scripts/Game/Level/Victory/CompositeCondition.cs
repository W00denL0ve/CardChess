using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 组合条件 — 递归嵌套 AND / OR
/// </summary>
[System.Serializable]
public class CompositeCondition : VictoryCondition
{
    public LogicOperator op;
    public List<VictoryCondition> children = new();

    public override void Initialize()
    {
        foreach (var c in children) c?.Initialize();
    }

    public override bool IsMet() => op switch
    {
        LogicOperator.And => children.All(c => c != null && c.IsMet()),
        LogicOperator.Or  => children.Any(c => c != null && c.IsMet()),
        _ => false
    };

    /// <summary>
    /// AND: 任一子条件不可达成 → 整体不可达成
    /// OR:  所有子条件都不可达成 → 整体不可达成
    /// </summary>
    public override bool IsImpossible() => op switch
    {
        LogicOperator.And => children.Any(c => c != null && c.IsImpossible()),
        LogicOperator.Or  => children.All(c => c == null || c.IsImpossible()),
        _ => false
    };

    public override void Cleanup()
    {
        foreach (var c in children) c?.Cleanup();
    }
}
