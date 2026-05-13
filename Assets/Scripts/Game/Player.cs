using UnityEngine;

public class Player : MonoBehaviour
{
    
    public int maxHandSize = 5;

    public void DrawCard()
    {
        DeckManager.Instance.DrawCard();
    }
}