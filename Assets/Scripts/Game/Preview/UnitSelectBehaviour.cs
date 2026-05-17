using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位选择预览行为 — 高亮候选单位、单击预选、双击确认
/// </summary>
public class UnitSelectBehaviour : PreviewBehaviour
{
    private PreviewStep step;
    private PreviewState currentState;

    public UnitSelectBehaviour()
    {
        Type = PreviewType.UnitSelect;
    }

    public override void OnEnter(PreviewStep step)
    {
        this.step = step;
        currentState = PreviewState.Selecting;

        if (step.CandidateUnits != null && step.CandidateUnits.Count > 0)
            UnitVisualizer?.HighlightUnits(step.CandidateUnits);

        Logger.Log($"UnitSelectBehaviour: 进入，候选 {step.CandidateUnits?.Count ?? 0} 个单位");
    }

    public override void ClearVisuals()
    {
        UnitVisualizer?.ClearHighlights();
        HideBox?.Invoke();
    }

    /// <summary>
    /// 压入嵌套步骤时保留被选单位的视觉效果（高亮 + 预选框），恢复其他候选单位
    /// </summary>
    public override void PauseVisuals()
    {
        if (step.PreselectedUnit != null)
        {
            var others = new List<Unit>(step.CandidateUnits);
            others.Remove(step.PreselectedUnit);
            UnitVisualizer?.ClearHighlightsOf(others);
        }
        else
        {
            UnitVisualizer?.ClearHighlights();
        }
        HideBox?.Invoke();
    }

    public override void RestoreVisuals(PreviewStep step)
    {
        if (step.CandidateUnits != null && step.CandidateUnits.Count > 0)
            UnitVisualizer?.HighlightUnits(step.CandidateUnits);

        if (step.PreselectedUnit != null)
        {
            ShowBoxOnUnit?.Invoke(step.PreselectedUnit);
            currentState = PreviewState.Preselected;
        }
    }

    // ==================== 输入事件 ====================

    public override void OnUnitClick(Unit unit)
    {
        if (!IsValidUnit(unit)) return;

        switch (currentState)
        {
            case PreviewState.Selecting:
                step.PreselectedUnit = unit;
                currentState = PreviewState.Preselected;
                ShowBoxOnUnit?.Invoke(unit);
                Logger.Log($"UnitSelectBehaviour: 预选单位 {unit.name}");
                break;

            case PreviewState.Preselected:
                if (unit != step.PreselectedUnit)
                {
                    step.PreselectedUnit = unit;
                    ShowBoxOnUnit?.Invoke(unit);
                    Logger.Log($"UnitSelectBehaviour: 更新预选 {unit.name}");
                }
                else
                {
                    // 再次点击已预选单位 → 确认选择
                    Logger.Log($"UnitSelectBehaviour: 确认选择 {unit.name}");
                    step.OnUnitConfirm?.Invoke(unit);
                }
                break;
        }
    }

    private Unit lastHoveredCandidateUnit;

    public override void OnHover(HoverInfo hover)
    {
        Unit current = hover.unit;
        if (current == lastHoveredCandidateUnit) return;
        lastHoveredCandidateUnit = current;

        if (current != null && step.CandidateUnits.Contains(current))
        {
            UnitVisualizer?.SetHoverUnit(current);
        }
        else
        {
            UnitVisualizer?.ClearHoverUnit();
        }
    }

    // ==================== 内部方法 ====================

    bool IsValidUnit(Unit unit)
    {
        if (step.CandidateUnits == null || !step.CandidateUnits.Contains(unit))
        {
            Logger.LogWarning($"UnitSelectBehaviour: 单位 {unit.name} 不在候选中");
            return false;
        }
        return true;
    }
}
