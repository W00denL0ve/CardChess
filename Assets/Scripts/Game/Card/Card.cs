using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 卡牌视觉表现 — 纯 UI 层
/// 不包含任何效果执行逻辑
/// </summary>
public class CardVisualizer : MonoBehaviour, IPointerClickHandler
{
    public CardData cardData;
    public Image artworkImage;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI descriptionTMP;

    public void Bind(CardData data)
    {
        cardData = data;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (cardData == null) return;
        if (artworkImage != null) artworkImage.sprite = cardData.artwork;
        if (nameTMP != null) nameTMP.text = cardData.cardName;
        if (descriptionTMP != null) descriptionTMP.text = cardData.description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData == null) return;
        GameEventChannel.Dispatch(new CardClickedEvent(cardData));
    }
}