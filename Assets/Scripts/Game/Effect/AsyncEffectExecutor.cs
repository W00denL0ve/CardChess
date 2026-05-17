using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 异步效果执行器 — 按 EffectStep 序列逐步骤执行
///
/// 核心逻辑（链式上下文传递）：
///   初始 executor = executed = 卡牌发出者
///   每出现一个目标选择器：executor ← old executed, executed ← 目标
///
/// 时序：
///   OnExecute → 数据变更（对齐表现"打击瞬间"）
///   OnComplete → 效果完全结束（对齐表现完全结束）
/// </summary>
public class AsyncEffectExecutor : MonoBehaviour
{
    public static AsyncEffectExecutor Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 异步执行卡牌的所有效果链
    /// 每条链独立从初始源开始执行上下文链
    /// </summary>
    public void ExecuteCardChainsAsync(CardData card, ITarget initialSource, Action onComplete = null)
    {
        if (card == null) { onComplete?.Invoke(); return; }

        var ctx = new EffectContext
        {
            sourceCard = card,
            executor = initialSource,
            executed = initialSource
        };
        StartCoroutine(ExecuteAllChainsRoutine(card.chains, ctx, onComplete));
    }

    /// <summary>
    /// 依顺序执行多条效果链，每条链从初始上下文开始
    /// </summary>
    IEnumerator ExecuteAllChainsRoutine(List<EffectChain> chains, EffectContext baseContext, Action onComplete)
    {
        if (chains == null) { onComplete?.Invoke(); yield break; }

        foreach (var chain in chains)
        {
            if (chain == null || chain.steps == null || chain.steps.Count == 0) continue;
            yield return ExecuteStepsRoutine(chain.steps, baseContext, null);
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 从指定上下文开始执行 steps 序列
    /// </summary>
    public void ExecuteStepsAsync(List<GameEffectStep> steps, EffectContext context, Action onComplete = null)
    {
        StartCoroutine(ExecuteStepsRoutine(steps, context, onComplete));
    }

    /// <summary>
    /// 异步执行单个步骤
    /// </summary>
    public void ExecuteStepAsync(GameEffectStep step, EffectContext context, Action onComplete = null)
    {
        StartCoroutine(ExecuteSingleStepRoutine(step, context, onComplete));
    }

    // ====================================================================
    //  协程
    // ====================================================================

    IEnumerator ExecuteStepsRoutine(List<GameEffectStep> steps, EffectContext context, Action onComplete)
    {
        if (steps == null) { onComplete?.Invoke(); yield break; }

        EffectContext currentCtx = context;
        foreach (var step in steps)
        {
            if (step == null) continue;
            yield return ResolveAndExecute(step, currentCtx, (nextCtx) => currentCtx = nextCtx);
        }

        onComplete?.Invoke();
    }

    IEnumerator ExecuteSingleStepRoutine(GameEffectStep step, EffectContext context, Action onComplete)
    {
        EffectContext resultCtx = context;
        yield return ResolveAndExecute(step, context, (nextCtx) => resultCtx = nextCtx);
        onComplete?.Invoke();
    }

    // ====================================================================
    //  步骤解析：选择器 → 效果
    // ====================================================================

    /// <summary>
    /// 解析并执行一个步骤。
    /// onContextUpdated 回调返回更新后的上下文（选择器可能改变了 executor/executed）
    /// </summary>
    IEnumerator ResolveAndExecute(GameEffectStep step, EffectContext context, Action<EffectContext> onContextUpdated)
    {
        EffectContext newCtx = context;

        // ── 选择器阶段 ──
        if (step.selector != null)
        {
            yield return ResolveSelector(step.selector, context,
                (selectedTarget) =>
                {
                    if (selectedTarget != null)
                    {
                        newCtx = new EffectContext
                        {
                            sourceCard = context.sourceCard,
                            executor = step.selector.ChangesContext ? context.executed : context.executor,
                            executed = selectedTarget,
                            customParams = context.customParams
                        };
                    }
                });
        }

        // ── 效果阶段 ──
        if (step.effect != null)
        {
            step.effect.OnExecute(newCtx);
            step.effect.OnComplete(newCtx);
        }

        onContextUpdated?.Invoke(newCtx);
    }

    // ====================================================================
    //  选择器解析
    // ====================================================================

    /// <summary>
    /// 解析一个选择器：自动选择器直接返回，手动选择器进入 PreviewManager 等待玩家
    /// </summary>
    IEnumerator ResolveSelector(TargetSelector selector, EffectContext context,
        Action<ITarget> onSelected)
    {
        if (selector is ManualCellSelector cellSel)
        {
            yield return ResolveManualCell(cellSel, context, onSelected);
        }
        else if (selector is ManualUnitSelector unitSel)
        {
            yield return ResolveManualUnit(unitSel, context, onSelected);
        }
        else
        {
            // 自动选择器：直接取第一个目标
            var targets = selector.GetTargets(context);
            if (targets != null && targets.Count > 0)
            {
                // 自动高亮（短暂显示后被效果覆盖）
                selector.PreviewHighlight(context, true);
                onSelected(targets[0]);
            }
            else
            {
                Logger.LogWarning($"[AsyncEffect] {selector.name} 返回空目标");
                onSelected(null);
            }
        }
    }

    // ====================================================================
    //  手动选择器
    // ====================================================================

    IEnumerator ResolveManualCell(ManualCellSelector selector, EffectContext context,
        Action<ITarget> onSelected)
    {
        Unit execUnit = context.GetExecutorUnit();
        if (execUnit == null) { onSelected(null); yield break; }

        int range = selector.range >= 0 ? selector.range : execUnit.ActionPointLimit;
        if (selector.clampToActionPointLimit)
            range = Mathf.Min(range, execUnit.ActionPointLimit);

        var candidates = GridManager.Instance?.GetReachableCells(
            execUnit.GridPosition, range,
            selector.ignoreOccupied, selector.canPassUnwalkable
        );
        if (candidates == null || candidates.Count == 0)
        {
            Logger.LogWarning($"[AsyncEffect] ManualCell: 无可用候选格子");
            onSelected(null);
            yield break;
        }

        if (!selector.includeOrigin)
            candidates.Remove(execUnit.GridPosition);

        bool completed = false;
        Vector2Int selectedPos = execUnit.GridPosition;

        PreviewManager.Instance.EnterGridPreview(execUnit, candidates,
            (pos) => { selectedPos = pos; completed = true; },
            () => { completed = true; }
        );

        yield return new WaitUntil(() => completed);
        Logger.Log($"[AsyncEffect] ManualCell: 选定 ({selectedPos.x},{selectedPos.y})");
        onSelected(new CellTarget(selectedPos));
    }

    IEnumerator ResolveManualUnit(ManualUnitSelector selector, EffectContext context,
        Action<ITarget> onSelected)
    {
        Unit execUnit = context.GetExecutorUnit();
        if (execUnit == null) { onSelected(null); yield break; }

        var candidates = GetUnitCandidates(execUnit, selector);
        if (candidates == null || candidates.Count == 0)
        {
            Logger.LogWarning($"[AsyncEffect] ManualUnit: 无可用候选单位");
            onSelected(null);
            yield break;
        }

        bool completed = false;
        Unit selectedUnit = null;

        PreviewManager.Instance.EnterUnitPreview(candidates,
            (unit) => { selectedUnit = unit; completed = true; },
            () => { completed = true; }
        );

        yield return new WaitUntil(() => completed);

        if (selectedUnit != null)
        {
            Logger.Log($"[AsyncEffect] ManualUnit: 选定 {selectedUnit.UnitId}");
            onSelected(new UnitTarget(selectedUnit));
        }
        else
        {
            onSelected(null);
        }
    }

    List<Unit> GetUnitCandidates(Unit executor, ManualUnitSelector selector)
    {
        var lm = LevelManager.Instance;
        if (lm == null) return null;

        switch (selector.candidateType)
        {
            case ManualUnitSelector.CandidateType.Enemies: return lm.GetEnemiesOf(executor);
            case ManualUnitSelector.CandidateType.Allies: return lm.GetAlliesOf(executor);
            case ManualUnitSelector.CandidateType.All: return lm.AllUnits.Where(u => u.IsAlive).ToList();
            case ManualUnitSelector.CandidateType.SameFaction: return lm.GetUnitsOf(selector.targetFaction);
            default: return lm.GetEnemiesOf(executor);
        }
    }
}
