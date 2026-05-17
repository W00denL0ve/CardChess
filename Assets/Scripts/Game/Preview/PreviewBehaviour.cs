using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 预览状态
/// </summary>
public enum PreviewState { Idle, Selecting, Preselected }

/// <summary>
/// 预览类型标识
/// </summary>
public enum PreviewType { None, GridMove, UnitSelect }

/// <summary>
/// 预览步骤数据 — 栈中的一个层级
/// </summary>
public class PreviewStep
{
    public PreviewType Type;
    public List<Vector2Int> CandidateCells;
    public List<Unit> CandidateUnits;
    public Unit CurrentUnit;
    public Vector2Int? PreselectedCell;
    public Unit PreselectedUnit;
    public Action<Vector2Int> OnCellConfirm;
    public Action<Unit> OnUnitConfirm;
    public Action OnCancel;
}

/// <summary>
/// 预览行为基类 — 每个预览类型的行为封装
/// 子类必须实现具体逻辑：高亮/悬停/点击/双击/确认/取消
/// </summary>
public abstract class PreviewBehaviour
{
    public PreviewType Type { get; protected set; }

    // 由 PreviewManager 注入的引用
    protected GridVisualizer GridVisualizer { get; private set; }
    protected UnitVisualizer UnitVisualizer { get; private set; }
    protected PathRenderer PathRenderer { get; private set; }
    protected Func<HoverInfo> HoverProvider { get; private set; }

    // 公共视觉工具（由 PreviewManager 提供）
    protected Action<Vector2Int> ShowPreselectAt { get; private set; }
    protected Action HidePreselect { get; private set; }
    protected Action<Unit> ShowBoxOnUnit { get; private set; }
    protected Action HideBox { get; private set; }

    public void Initialize(
        GridVisualizer gridVis, UnitVisualizer unitVis, PathRenderer pathRend,
        Func<HoverInfo> hoverProvider,
        Action<Vector2Int> showMarker, Action hideMarker,
        Action<Unit> showBox, Action hideBox)
    {
        GridVisualizer = gridVis;
        UnitVisualizer = unitVis;
        PathRenderer = pathRend;
        HoverProvider = hoverProvider;
        ShowPreselectAt = showMarker;
        HidePreselect = hideMarker;
        ShowBoxOnUnit = showBox;
        HideBox = hideBox;
    }

    // ==================== 生命周期 ====================
    public abstract void OnEnter(PreviewStep step);
    public abstract void ClearVisuals();
    public abstract void RestoreVisuals(PreviewStep step);

    /// <summary>
    /// 当在上层压入新步骤时调用，默认等同于 ClearVisuals，子类可重写以保留被选目标的视觉效果
    /// </summary>
    public virtual void PauseVisuals() { ClearVisuals(); }

    // ==================== 输入事件 ====================
    public virtual void OnCellClick(Vector2Int pos) { }
    public virtual void OnUnitClick(Unit unit) { }
    public virtual void OnHover(HoverInfo hover) { }
}
