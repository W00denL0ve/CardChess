using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public CardData cardData;
    public Image artworkImage;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI descriptionTMP;

    private void Start()
    {
        UpdateUI(); // 确保在Start中更新UI，以防cardData在Inspector中设置后没有立即更新UI
        UpdateCardData(); // 根据角色状态更新需要计算相对值的CardData属性；根据存档的卡牌状态更新CardData属性（如是否升级）
    }

    public void UpdateUI()
    {
        if (cardData != null)
        {
            artworkImage.sprite = cardData.artwork;
            nameTMP.text = cardData.cardName;
            descriptionTMP.text = cardData.description;
        }
    }

    public void UpdateCardData()
    {
        //todo 根据角色状态更新需要计算相对值的CardData属性；根据存档的卡牌状态更新CardData属性（如是否升级）
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // PreviewManager.Instance.PreviewCard(cardData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // PreviewManager.Instance.ClearPreview();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        GameManager.Instance.OnCardPlayed(cardData);
    }
}