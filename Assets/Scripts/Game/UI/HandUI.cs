using System.Collections.Generic;
using UnityEngine;

public class HandUI : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform handContainer;
    public float cardSpacing = 100f;

    private List<Card> handCards = new List<Card>();

    public void AddCard(CardData cardData)
    {
        GameObject cardGO = Instantiate(cardPrefab, handContainer);
        Card card = cardGO.GetComponent<Card>();
        card.cardData = cardData;
        card.UpdateUI();
        handCards.Add(card);
        LayoutCards();
    }

    public void RemoveCard(CardData cardData)
    {
        Card toRemove = handCards.Find(c => c.cardData == cardData);
        if (toRemove != null)
        {
            handCards.Remove(toRemove);
            Destroy(toRemove.gameObject);
            LayoutCards();
        }
    }

    private void LayoutCards()
    {
        int count = handCards.Count;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * cardSpacing;
            handCards[i].transform.localPosition = new Vector3(x, 0, 0);
        }
    }
}