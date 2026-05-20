using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class CardHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CardVisualizer card;

    public void OnPointerEnter(PointerEventData eventData) => card?.OnPointerEnter(eventData);
    public void OnPointerExit(PointerEventData eventData) => card?.OnPointerExit(eventData);
}