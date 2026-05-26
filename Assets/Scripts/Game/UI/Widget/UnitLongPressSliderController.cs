using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 长按环形 Slider 控制器
/// 监听长按事件，在目标屏幕位置显示环形进度条，随长按进度填充
/// </summary>
public class UnitLongPressSliderController : MonoBehaviour
{
    [Header("Slider 预制体")]
    [SerializeField] private GameObject ringSliderPrefab;

    [Header("屏幕位置偏移")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 50);

    private GameObject currentSlider;
    private Image ringSliderImage;
    private Canvas sliderCanvas;

    private Transform systemLayer;

    private void OnEnable()
    {
        GameEventChannel.Register<LongPressStartedEvent>(OnLongPressStarted);
        GameEventChannel.Register<LongPressUpdateEvent>(OnLongPressUpdate);
        GameEventChannel.Register<LongPressCancelledEvent>(OnLongPressCancelled);
        GameEventChannel.Register<LongPressPerformedEvent>(OnLongPressPerformed);
    }

    private void OnDisable()
    {
        GameEventChannel.Unregister<LongPressStartedEvent>(OnLongPressStarted);
        GameEventChannel.Unregister<LongPressUpdateEvent>(OnLongPressUpdate);
        GameEventChannel.Unregister<LongPressCancelledEvent>(OnLongPressCancelled);
        GameEventChannel.Unregister<LongPressPerformedEvent>(OnLongPressPerformed);
    }

    private void Start()
    {
        // 如果不在 Canvas 下，通过 UIManager 获取 Canvas
        if (sliderCanvas == null)
            sliderCanvas = GetComponentInParent<Canvas>();
        if (sliderCanvas == null && UIManager.Instance != null)
            sliderCanvas = UIManager.Instance.MainCanvas;

        // 找到 SystemLayer 作为 Slider 的父级
        if (sliderCanvas != null)
            systemLayer = sliderCanvas.transform.Find("SystemLayer");

        // 提前实例化 Slider，后续只控制显隐
        if (ringSliderPrefab != null && systemLayer != null)
        {
            currentSlider = Instantiate(ringSliderPrefab, systemLayer);
            currentSlider.name = "LongPressRingSlider";

            var circularBar = currentSlider.GetComponent<CircularBar>();
            ringSliderImage = circularBar?.fillImage;
            if (ringSliderImage != null)
                ringSliderImage.type = Image.Type.Filled;

            currentSlider.SetActive(false);
        }
    }

    private void OnLongPressStarted(LongPressStartedEvent evt)
    {
        if (currentSlider == null || ringSliderImage == null) return;

        currentSlider.SetActive(true);
        ringSliderImage.fillAmount = 0f;
        UpdateSliderPosition(evt.Target.GetScreenPosition());
    }

    private void OnLongPressUpdate(LongPressUpdateEvent evt)
    {
        if (currentSlider == null || ringSliderImage == null) return;

        ringSliderImage.fillAmount = evt.Progress;
        UpdateSliderPosition(evt.Target.GetScreenPosition());
    }

    private void OnLongPressCancelled(LongPressCancelledEvent evt)
    {
        HideSlider();
    }

    private void OnLongPressPerformed(LongPressPerformedEvent evt)
    {
        HideSlider();
    }

    private void UpdateSliderPosition(Vector3 screenPos)
    {
        if (currentSlider == null || sliderCanvas == null) return;

        RectTransform rect = currentSlider.GetComponent<RectTransform>();
        if (rect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sliderCanvas.GetComponent<RectTransform>(),
                screenPos,
                sliderCanvas.worldCamera,
                out Vector2 localPos);

            rect.anchoredPosition = localPos + screenOffset;
        }
    }

    private void HideSlider()
    {
        if (currentSlider != null)
            currentSlider.SetActive(false);
    }
}
