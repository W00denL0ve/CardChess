using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int maxHandSize = 5;

    public void DrawCard()
    {
        // 走 DrawCardsAsync 确保手牌上限检测
        DeckManager.Instance.StartCoroutine(DeckManager.Instance.DrawCardsAsync(1));
    }
}