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
    /// initialSource 自动包装为 CardTarget(card)
    /// 完成后自动调用 DeckManager.CompleteCard
    /// </summary>
    public void ExecuteCardChainsAsync(CardData card, Action onComplete = null)
    {
        if (card == null) { Logger.Log("[AsyncEffect] card is null"); onComplete?.Invoke(); return; }

        Logger.Log($"[AsyncEffect] 开始执行卡牌效果链: {card.cardName}，链数量: {card.chains?.Count ?? 0}");

        var source = new CardTarget(card);
        var ctx = new EffectContext
        {
            sourceCard = card,
            executor = source,
            executed = source
        };
        StartCoroutine(ExecuteAllChainsRoutine(card.chains, ctx, () =>
        {
            Logger.Log($"[AsyncEffect] 所有效果链完成，调用 CompleteCard");
            DeckManager.Instance?.CompleteCard(card);
            Logger.Log($"[AsyncEffect] onComplete 回调");
            onComplete?.Invoke();
        }));
    }

    /// <summary>
    /// 依顺序执行多条效果链，每条链从初始上下文开始
    /// </summary>
    IEnumerator ExecuteAllChainsRoutine(List<EffectChain> chains, EffectContext baseContext, Action onComplete)
    {
        if (chains == null) { Logger.Log("[ExecuteAllChains] chains 为 null，直接完成"); onComplete?.Invoke(); yield break; }

        Logger.Log($"[ExecuteAllChains] 开始执行 {chains.Count} 条链");

        foreach (var chain in chains)
        {
            if (chain == null || chain.steps == null || chain.steps.Count == 0)
            {
                Logger.Log("[ExecuteAllChains] 跳过空链");
                continue;
            }

            Logger.Log($"[ExecuteAllChains] 执行链，步骤数: {chain.steps.Count}");

            // 每条链独立创建 context 副本，避免链间互扰
            var ctx = new EffectContext
            {
                sourceCard = baseContext.sourceCard,
                executor = baseContext.executor,
                executed = baseContext.executed
            };
            yield return ExecuteStepsRoutine(chain.steps, ctx, null);
        }

        Logger.Log("[ExecuteAllChains] 所有链执行完毕");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 从指定上下文开始执行 steps 序列
    /// </summary>
    public void ExecuteStepsAsync(List<ChainStep> steps, EffectContext context, Action onComplete = null)
    {
        StartCoroutine(ExecuteStepsRoutine(steps, context, onComplete));
    }

    /// <summary>
    /// 异步执行单个步骤
    /// </summary>
    public void ExecuteStepAsync(ChainStep step, EffectContext context, Action onComplete = null)
    {
        StartCoroutine(ExecuteSingleStepRoutine(step, context, onComplete));
    }

    // ====================================================================
    //  协程
    // ====================================================================

    /// <summary>
    /// AI 专用 — 返回协程 IEnumerator，调用方可 yield 等待执行完成
    /// </summary>
    public IEnumerator ExecuteChainAI(List<ChainStep> steps, EffectContext context)
    {
        yield return ExecuteStepsRoutine(steps, context, null);
    }

    IEnumerator ExecuteStepsRoutine(List<ChainStep> steps, EffectContext context, Action onComplete)
    {
        if (steps == null) { onComplete?.Invoke(); yield break; }

        Logger.Log($"[ExecuteSteps] 开始执行 {steps.Count} 个步骤");

        bool prevWasSelector = false;
        foreach (var step in steps)
        {
            if (step == null) continue;
            Logger.Log($"[ExecuteSteps] 执行步骤: {step.GetType().Name}");
            yield return ResolveAndExecute(step, context, prevWasSelector);
            if (context.chainBroken)
            {
                Logger.Log("[ExecuteSteps] 链中断");
                PreviewManager.Instance?.ClearAll();
                break;
            }
            prevWasSelector = step is SelectorStep;

            // 非效果步骤 → 高亮当前被执行者（上一步选中的目标）
            if (!(step is EffectStep))
                HighlightExecuted(context);
        }

        Logger.Log("[ExecuteSteps] 步骤序列完成");
        onComplete?.Invoke();
    }

    IEnumerator ExecuteSingleStepRoutine(ChainStep step, EffectContext context, Action onComplete)
    {
        yield return ResolveAndExecute(step, context);
        onComplete?.Invoke();
    }

    // ====================================================================
    //  步骤解析：选择器 → 效果
    // ====================================================================

    /// <summary>
    /// 解析并执行一个步骤。
    /// EffectContext 为引用类型，所有步骤共享同一实例。
    /// 条件不满足时中断整条链。返回 false 表示链应中断。
    /// </summary>
    IEnumerator ResolveAndExecute(ChainStep step, EffectContext context, bool prevWasSelector = false)
    {
        context.ClearStepCache();

        if (step is SelectorStep ss)
        {
            yield return ResolveSelectorStep(ss, context, prevWasSelector);
        }
        else if (step is ConditionStep cs)
        {
            if (!ResolveConditionStep(cs, context))
            {
                yield break; // 中断，由外层检查
            }
        }
        else if (step is EffectStep es)
        {
            yield return ResolveEffectStep(es, context);
        }
    }

    IEnumerator ResolveSelectorStep(SelectorStep step, EffectContext context, bool prevWasSelector = false)
    {
        if (step.selector == null) yield break;
        // 上一步是选择器 → 玩家可回退
        context.canRevert = prevWasSelector;
        ITarget selectedTarget = null;
        yield return ResolveSelector(step.selector, context, (t) => selectedTarget = t);
        if (selectedTarget != null)
        {
            if (step.selector.chooseExecutor) // 如果选择器选执行者，改变执行者。
            {
                context.executor = selectedTarget;
            }
            if (step.selector.chooseExecuted) // 如果选择器选择被执行者，改变被执行者。
            {
                context.executed = selectedTarget;
            }
        }
        else
        {
            // 未选中任何目标 → 中断整条链
            context.chainBroken = true;
        }
    }

    bool ResolveConditionStep(ConditionStep step, EffectContext context)
    {
        if (step.condition == null) return true;
        if (step.condition.IsMet(context)) return true;
        Logger.Log($"[AsyncEffect] 条件 '{step.condition.name}' 未满足，链中断");
        context.chainBroken = true;
        return false;
    }

    IEnumerator ResolveEffectStep(EffectStep step, EffectContext context)
    {
        if (step.effect == null) yield break;
        Logger.Log($"[ResolveEffectStep] 执行效果: {step.effect.name}");
        PreviewManager.Instance?.ClearAll();
        step.effect.OnExecute(context);
        if (step.effect is IAnimatedEffect anim)
        {
            Logger.Log("[ResolveEffectStep] 播放动画");
            yield return anim.PlayAnimation(context);
            Logger.Log("[ResolveEffectStep] 动画完成");
        }
        step.effect.OnComplete(context);
        Logger.Log("[ResolveEffectStep] 效果执行完毕");
    }

    /// <summary>高亮当前被执行者单位（不含标记）</summary>
    private void HighlightExecuted(EffectContext context)
    {
        Unit unit = context.GetExecutedUnit();
        if (unit != null && unit.IsAlive)
            UnitVisualizer.Instance?.HighlightUnits(new List<Unit> { unit });
    }

    // ====================================================================
    //  选择器解析 — 统一流程
    //  候选=1 → 自动选择；候选>1 → 根据类型进入预览
    // ====================================================================

    IEnumerator ResolveSelector(TargetSelector selector, EffectContext context,
        Action<ITarget> onSelected)
    {
        var candidates = selector.GetTargets(context);
        if (candidates == null || candidates.Count == 0)
        {
            Logger.Log($"[AsyncEffect] {selector.name} 返回空目标列表");
            onSelected(null);
            yield break;
        }

        ITarget selected = null;
        bool completed = false;
        bool autoConfirm = candidates.Count == 1;

        // AI 模式：跳过预览，直接选目标
        if (context.aiSelector != null)
        {
            selected = context.aiSelector(candidates);
            if (selected == null) selected = candidates[0];
            Logger.Log($"[AI] {selector.name} 选定目标");
            onSelected(selected);
            yield break;
        }

        var first = candidates[0];
        if (first is CellTarget)
        {
            Unit execUnit = context.GetExecutedUnit();
            var cellCandidates = candidates
                .Select(t => t.GetCellPosition())
                .Where(p => p.HasValue).Select(p => p.Value).ToList();

            PreviewManager.Instance.StartCellPreview(execUnit, cellCandidates,
                (pos) =>
                {
                    var path = GridManager.Instance?.FindPath(execUnit.GridPosition, pos);
                    if (path != null && path.Count > 0)
                        context.cachedPath = path;
                    selected = new CellTarget(pos);
                    completed = true;
                },
                context.canRevert
            );

            // 高亮当前被执行者（和候选格子同时显示）
            HighlightExecuted(context);

            if (autoConfirm)
            {
                Logger.Log($"[AsyncEffect] {selector.name} 自动确认 (唯一格子候选)");
                PreviewManager.Instance.AutoConfirmSelection();
            }
        }
        else if (first is UnitTarget)
        {
            var unitCandidates = candidates
                .Select(t => (t as UnitTarget)?.unit)
                .Where(u => u != null).ToList();

            PreviewManager.Instance.StartUnitPreview(unitCandidates,
                (unit) =>
                {
                    selected = new UnitTarget(unit);
                    completed = true;
                },
                context.canRevert
            );

            // 高亮当前被执行者（和候选单位同时显示）
            HighlightExecuted(context);

            if (autoConfirm)
            {
                Logger.Log($"[AsyncEffect] {selector.name} 自动确认 (唯一单位候选)");
                PreviewManager.Instance.AutoConfirmSelection();
            }
        }
        else
        {
            Logger.LogWarning($"[AsyncEffect] {selector.name} 无法识别的候选类型");
            onSelected(null);
            yield break;
        }

        yield return new WaitUntil(() => completed);

        Logger.Log($"[AsyncEffect] {selector.name} 选定目标");
        onSelected(selected);
    }
}
