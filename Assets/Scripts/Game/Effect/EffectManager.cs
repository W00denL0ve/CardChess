using System.Collections;
using UnityEngine;

/// <summary>
/// 效果管理器 - 负责执行卡牌效果链（同步版本，用于不需要手动选择的场景）
/// 手动选择场景请使用 AsyncEffectExecutor
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 同步执行卡牌的所有效果链（仅包含自动选择器的步骤）
    /// </summary>
    public void ExecuteCardChains(CardData card, EffectContext context)
    {
        if (card == null || card.chains == null) return;

        foreach (var chain in card.chains)
        {
            if (chain == null || chain.steps == null || chain.steps.Count == 0) continue;

            // 每条链从原始上下文开始（引用类型，需要浅拷贝）
            var ctx = new EffectContext
            {
                sourceCard = context.sourceCard,
                executor = context.executor,
                executed = context.executed
            };

            foreach (var step in chain.steps)
            {
                if (step == null) continue;

                ctx.ClearStepCache();

                if (step is SelectorStep ss)
                {
                    if (ss.selector == null) continue;
                    var targets = ss.selector.GetTargets(ctx);
                    if (targets == null || targets.Count == 0) continue;

                    ctx.executor = ss.selector.chooseExecutor ? ctx.executed : ctx.executor;
                    ctx.executed = targets[0];
                }
                else if (step is ConditionStep cs)
                {
                    if (cs.condition != null && !cs.condition.IsMet(ctx))
                    {
                        Logger.Log($"[EffectManager] 条件 '{cs.condition.name}' 未满足，链中断");
                        break;
                    }
                }
                else if (step is EffectStep es)
                {
                    if (es.effect != null)
                    {
                        es.effect.OnExecute(ctx);
                        es.effect.OnComplete(ctx);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 直接执行单个效果
    /// </summary>
    public void ExecuteEffect(Effect effect, EffectContext context)
    {
        effect?.OnExecute(context);
        effect?.OnComplete(context);
    }
}
