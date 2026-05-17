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

            EffectContext currentCtx = context;
            foreach (var step in chain.steps)
            {
                if (step == null) continue;

                // 选择器
                if (step.selector != null)
                {
                    var targets = step.selector.GetTargets(currentCtx);
                    if (targets == null || targets.Count == 0) continue;

                    var newExecuted = targets[0];
                    currentCtx = new EffectContext
                    {
                        sourceCard = currentCtx.sourceCard,
                        executor = step.selector.ChangesContext ? currentCtx.executed : currentCtx.executor,
                        executed = newExecuted,
                        customParams = currentCtx.customParams
                    };
                }

                // 效果
                if (step.effect != null)
                {
                    step.effect.OnExecute(currentCtx);
                    step.effect.OnComplete(currentCtx);
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
