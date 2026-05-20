using UnityEngine;

/// <summary>
/// 移动效果 - 将 context.executor 移动到 context.executed 所在格子
/// </summary>
[CreateAssetMenu(fileName = "GiveMovePointEffect", menuName = "CardChess/EffectChain/Effects/GiveMovePoint")]
public class GiveMovePointEffect : Effect
{
    /// <summary>是否忽视行动力上限</summary>
    public bool ignoreLimit = false;
    public int points = 3;

    /// <summary>
    /// 执行时使Executed对象获得行动力
    /// </summary>
    /// <param name="context"></param>
    public override void OnExecute(EffectContext context)
    {
        context.GetExecutedUnit().AcquireMovePoint(points, ignoreLimit, context);
    }
}