using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;          // 引入 Button 命名空间
using DG.Tweening;

public class ButtonTween : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler
{
    [Header("缩放设置")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private bool ignoreTimeScale = true;

    private Vector3 originalScale;
    private RectTransform rectTransform;
    private Button button;          // 引用 Button 组件（可选）
    private string tweenId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        tweenId = $"ButtonScale_{GetInstanceID()}";
        
        // 尝试获取 Button 组件（若挂载的对象本身有 Button 组件）
        button = GetComponent<Button>();
    }

    // 判断是否可交互（如果没有 Button 组件，默认允许交互）
    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private void DoScale(Vector3 targetScale)
    {
        DOTween.Kill(tweenId);
        transform.DOScale(targetScale, duration)
                 .SetUpdate(ignoreTimeScale)
                 .SetId(tweenId);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!IsInteractable()) return;
        DoScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!IsInteractable()) return;
        DoScale(originalScale);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!IsInteractable()) return;
        DoScale(originalScale * pressScale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!IsInteractable()) return;
        if (IsPointerOverButton(e))
            DoScale(originalScale * hoverScale);
        else
            DoScale(originalScale);
    }

    private bool IsPointerOverButton(PointerEventData e)
    {
        Camera cam = e.pressEventCamera; 
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, e.position, cam
        );
    }
}