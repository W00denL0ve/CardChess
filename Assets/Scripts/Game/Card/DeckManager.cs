using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    // 事件
    public event Action<CardData> OnCardDrawn;
    public event Action<CardData> OnCardPlayed;

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
    public void DrawCard()
    {
        if (deck.Count == 0) return;
        CardData card = deck[UnityEngine.Random.Range(0, deck.Count)];
        deck.Remove(card);
        hand.Add(card);
        OnCardDrawn?.Invoke(card);
    }

    // ═══ 打出 ═══
    /// <summary>标记卡牌为已打出（手牌→pending），立即触发 OnCardPlayed</summary>
    public void MarkCardPlayed(CardData card)
    {
        if (!hand.Contains(card)) return;
        hand.Remove(card);
        pendingPlay.Add(card);
        OnCardPlayed?.Invoke(card);
    }

    /// <summary>效果链全部完成，根据 destination 决定去向</summary>
    public void CompleteCard(CardData card)
    {
        if (!pendingPlay.Contains(card)) return;
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

