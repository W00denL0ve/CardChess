using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [Header("牌库上限")]
    public int maxHandSize = 10;

    // 牌库
    public List<CardData> deck = new();
    public List<CardData> hand = new();
    public List<CardData> discardPile = new();
    public List<CardData> destroyedPile = new();

    // 已打出但效果链未执行完的卡牌
    private List<CardData> pendingPlay = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ═══ 手牌检测 ═══
    public bool IsCardInHand(CardData card) => hand.Contains(card);

    // ═══ 抽牌 ═══
    /// <summary>数据层：抽一张牌进手牌（内部方法，外部请调用 DrawCardsAsync）</summary>
    private void DrawCard()
    {
        if (deck.Count == 0) return;
        if (hand.Count >= maxHandSize) return;
        CardData card = deck[UnityEngine.Random.Range(0, deck.Count)];
        deck.Remove(card);
        hand.Add(card);
        HandUI.Instance.UpdateDrawPileDisplay(deck.Count);
        GameEventChannel.Dispatch(new CardDrawnEvent(card));
    }

    /// <summary>
    /// 数据层先行 + 表现层群组动画 — 一次性抽多张牌，动画完成后回调
    /// 牌库不足时自动洗回弃牌堆，并播放数字滚动动画
    /// </summary>
    public IEnumerator DrawCardsAsync(int count, Action onComplete = null)
    {
        // 牌库不足 → 洗回弃牌堆
        if (deck.Count < count && discardPile.Count > 0)
        {
            int oldCount = deck.Count;
            deck.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDeck();

            // 数字滚动动画
            if (HandUI.Instance != null)
                yield return HandUI.Instance.PlayDeckCountAnimation(oldCount, deck.Count);
        }

        // 限制抽牌数不超过手牌上限
        int space = maxHandSize - hand.Count;
        int actualCount = Mathf.Min(count, space);
        if (actualCount <= 0) { onComplete?.Invoke(); yield break; }

        // 数据层：全部同步完成
        for (int i = 0; i < actualCount; i++)
            DrawCard();

        // 表现层：群组飞入动画
        if (HandUI.Instance != null)
            yield return HandUI.Instance.PlayGroupDrawAnimation();

        onComplete?.Invoke();
    }

    /// <summary>
    /// 数据层先行 + 表现层群组动画 — 弃掉所有不保留的手牌，动画完成后回调
    /// </summary>
    public IEnumerator DiscardNonRetainedAsync(Action onComplete = null)
    {
        // 数据层：找出不保留的牌
        var toDiscard = hand.FindAll(c => !c.retain);
        if (toDiscard.Count == 0) { onComplete?.Invoke(); yield break; }

        foreach (var card in toDiscard)
            hand.Remove(card);

        // 通知 HandUI 标记视觉
        HandUI.Instance?.MarkCardsForDiscard(toDiscard);

        // 表现层
        if (HandUI.Instance != null)
            yield return HandUI.Instance.PlayDiscardAnimation();

        // 数据层：进入弃牌堆
        discardPile.AddRange(toDiscard);

        onComplete?.Invoke();
    }

    // ═══ 打出 ═══
    /// <summary>标记卡牌为已打出（手牌→pending），触发 CardPlayedEvent</summary>
    public void MarkCardPlayed(CardData card)
    {
        if (!hand.Contains(card)) return;
        hand.Remove(card);
        pendingPlay.Add(card);
        GameEventChannel.Dispatch(new CardPlayedEvent(card));
    }

    /// <summary>效果链全部完成，根据 destination 决定去向</summary>
    public void CompleteCard(CardData card)
    {
        Logger.Log($"[DeckManager] CompleteCard: {card.cardName}");
        if (!pendingPlay.Contains(card))
        {
            Logger.LogWarning($"[DeckManager] CompleteCard: {card.cardName} 不在 pendingPlay 中");
            return;
        }
        pendingPlay.Remove(card);

        switch (card.destination)
        {
            case DestinationOnPlay.Discard:
                discardPile.Add(card);
                break;
            case DestinationOnPlay.Destroy:
                destroyedPile.Add(card);
                break;
            case DestinationOnPlay.ReturnToHand:
                hand.Add(card);
                break;
        }

        // 卡牌视觉从待命区飞向去向位置
        if (HandUI.Instance != null)
            HandUI.Instance.StartCoroutine(HandUI.Instance.AnimateCardToDestination(card));
    }

    // ═══ 弃牌/销毁 ═══
    public void DiscardCard(CardData card)
    {
        if (hand.Contains(card)) hand.Remove(card);
        else if (pendingPlay.Contains(card)) pendingPlay.Remove(card);
        discardPile.Add(card);
    }

    public void DestroyCard(CardData card)
    {
        if (hand.Contains(card)) hand.Remove(card);
        else if (pendingPlay.Contains(card)) pendingPlay.Remove(card);
        else if (discardPile.Contains(card)) discardPile.Remove(card);
        destroyedPile.Add(card);
    }

    // ═══ 洗牌 ═══
    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int ri = UnityEngine.Random.Range(i, deck.Count);
            (deck[i], deck[ri]) = (deck[ri], deck[i]);
        }
    }
}

