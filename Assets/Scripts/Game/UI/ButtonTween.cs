using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonTween : MonoBehaviour, 
    IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler
{
    [Header("缩放设置")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float pressScale = 0.95f;
    [SerializeField] private float duration = 0.1f;
    [SerializeField] private bool ignoreTimeScale = true; // 默认忽略时间缩放，兼容暂停

    private Vector3 originalScale;
    private RectTransform rectTransform;

    private string tweenId; // 单独设置ID

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        // 使用对象实例ID或名称组合，确保每个按钮的ID唯一
        tweenId = $"ButtonScale_{GetInstanceID()}";
    }

    // ------ 统一动画控制 ------
    private void DoScale(Vector3 targetScale)
    {
        // 先杀死本按钮之前的缩放动画，避免堆积
        DOTween.Kill(tweenId);
        transform.DOScale(targetScale, duration)
                 .SetUpdate(ignoreTimeScale)   // 关键：ignoreTimeScale
                 .SetId(tweenId);             // 设置ID方便管理
    }

    // ------ 指针回调 ------
    public void OnPointerEnter(PointerEventData e)
    {
        DoScale(originalScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData e)
    {
        DoScale(originalScale);
    }

    public void OnPointerDown(PointerEventData e)
    {
        DoScale(originalScale * pressScale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        // 抬起时判断鼠标是否还在按钮区域内
        if (IsPointerOverButton(e))
            DoScale(originalScale * hoverScale); // 悬停
        else
            DoScale(originalScale);             // 离开
    }

    // ------ 辅助：判断指针是否在按钮矩形内 ------
    private bool IsPointerOverButton(PointerEventData e)
    {
        // 获取Canvas使用的相机（OverlayCanvas传null即可）
        Camera cam = e.pressEventCamera; 
        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform, e.position, cam
        );
    }
}