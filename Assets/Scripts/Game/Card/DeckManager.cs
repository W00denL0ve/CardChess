using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    public List<CardData> deck = new List<CardData>();
    public List<CardData> hand = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();
    public List<CardData> destroyedPile = new List<CardData>();

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

    public bool IsCardInHand(CardData card)
    {
        return hand.Contains(card);
    }

    public void DrawCard()
    {
        if (deck.Count == 0) return;

        CardData card = deck[Random.Range(0, deck.Count)];
        deck.Remove(card);
        hand.Add(card);
        // Notify UI update or events if needed
    }

    public void PlayCard(CardData card)
    {
        if (!hand.Contains(card)) return;

        hand.Remove(card);
        discardPile.Add(card);
    }

    public void DiscardCard(CardData card)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
            discardPile.Add(card);
        }
    }

    public void DestroyCard(CardData card)
    {
        if (hand.Contains(card))
        {
            hand.Remove(card);
        }
        else if (discardPile.Contains(card))
        {
            discardPile.Remove(card);
        }

        destroyedPile.Add(card);
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int randomIndex = Random.Range(i, deck.Count);
            CardData temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
}
