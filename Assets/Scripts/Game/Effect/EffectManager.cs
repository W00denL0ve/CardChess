using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 效果管理器 - 负责协调效果的执行
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 执行卡牌的所有效果
    /// </summary>
    public void ExecuteCardEffects(CardData card, EffectContext context)
    {
        if (card == null || card.effects == null) return;

        foreach (var effect in card.effects)
        {
            if (effect != null)
            {
                effect.Apply(context);
            }
        }
    }

    /// <summary>
    /// 直接执行单个效果
    /// </summary>
    public void ExecuteEffect(Effect effect, EffectContext context)
    {
        effect?.Apply(context);
    }
}
