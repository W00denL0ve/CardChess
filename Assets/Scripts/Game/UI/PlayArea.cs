using UnityEngine;

public class PlayArea : MonoBehaviour
{
    public void AddPlayedCard(Card card)
    {
        card.transform.SetParent(transform);
        // Disable interactions
        card.GetComponent<CanvasGroup>().interactable = false;
    }
}