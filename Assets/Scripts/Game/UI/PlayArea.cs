using UnityEngine;
using System;

[Obsolete]
public class PlayArea : MonoBehaviour
{
    public void AddPlayedCard(CardVisualizer card)
    {
        card.transform.SetParent(transform);
        // Disable interactions
        card.GetComponent<CanvasGroup>().interactable = false;
    }
}