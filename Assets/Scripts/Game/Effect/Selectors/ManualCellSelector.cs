using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手动单元格选择器 — 在效果执行前需要玩家从候选列表中选择一个格子
/// 由 AsyncEffectExecutor 手动解析，GetTargets() 在此路径不生效
/// </summary>
[CreateAssetMenu(menuName = "Game/TargetSelector/ManualCell")]
public class ManualCellSelector : TargetSelector
{
    [Header("候选范围")]
    [Tooltip("-1 = 使用执行者的 ActionPointLimit；0 = 仅起点；>0 = 固定步数")]
    public int range = -1;

    [Tooltip("为 true 时，最终步数取 range 和执行者 ActionPointLimit 的较小值")]
    public bool clampToActionPointLimit = true;

    [Tooltip("是否包含起点格子（当前单位所在格）")]
    public bool includeOrigin = true;

    [Tooltip("是否忽略路径中间格子的占据单位")]
    public bool ignoreOccupied = false;

    [Tooltip("是否允许穿过不可行走格子（但终点必须可行走）")]
    public bool canPassUnwalkable = false;

    /// <summary>
    /// 手动选择器在 GetTargets 中不返回结果（由 AsyncEffectExecutor 异步解析）
    /// </summary>
    public override List<ITarget> GetTargets(EffectContext context)
    {
        return new List<ITarget>();
    }
}
