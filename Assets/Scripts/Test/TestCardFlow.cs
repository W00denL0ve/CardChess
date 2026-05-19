using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 卡牌系统测试 — 验证抽牌→出牌→效果执行的完整流程
///
/// 用法：
///   1. 拖入若干 CardData 到 testCards 列表
///   2. 场景中需存在 DeckManager、HandUI、TurnManager、AsyncEffectExecutor
///   3. 运行后按空格键触发一回合（抽牌→出牌阶段）
///   4. 点击手牌中的卡牌即可打出
///   5. 点击"结束回合"按钮进入敌人阶段
/// </summary>
public class TestCardFlow : MonoBehaviour
{
    [Header("测试卡牌")]
    public List<CardData> testCards = new();

    [Header("每回合抽牌数")]
    public int drawCount = 3;

    [Header("自动触发")]
    public bool autoStartOnLoad = true;

    private bool initialized;

    void Start()
    {
        if (!Validate()) return;

        // 将测试卡牌加入牌库
        foreach (var card in testCards)
        {
            if (card != null)
                DeckManager.Instance.deck.Add(card);
        }

        Logger.Log($"[TestCardFlow] 牌库已初始化，共 {DeckManager.Instance.deck.Count} 张卡牌");

        if (autoStartOnLoad)
            StartCoroutine(DelayedStart());
    }

    void Update()
    {
        if (!initialized && Input.GetKeyDown(KeyCode.Space))
        {
            initialized = true;
            TurnManager.Instance.StartTurn();
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        initialized = true;
        TurnManager.Instance.StartTurn();
    }

    private bool Validate()
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("[TestCardFlow] 场景中缺少 DeckManager");
            return false;
        }
        if (TurnManager.Instance == null)
        {
            Debug.LogError("[TestCardFlow] 场景中缺少 TurnManager");
            return false;
        }
        if (FindObjectOfType<HandUI>() == null)
        {
            Debug.LogError("[TestCardFlow] 场景中缺少 HandUI");
            return false;
        }
        if (AsyncEffectExecutor.Instance == null)
        {
            Debug.LogError("[TestCardFlow] 场景中缺少 AsyncEffectExecutor");
            return false;
        }
        return true;
    }
}
