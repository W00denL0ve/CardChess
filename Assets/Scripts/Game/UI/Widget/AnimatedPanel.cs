using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // 如果使用 CanvasGroup 需要这个命名空间

[RequireComponent(typeof(CanvasGroup))]
public class AnimatedPanel : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float animationDuration = 0.4f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    // 初始缩放比例 (0 = 从无到有, 1 = 无缩放)
    [SerializeField, Range(0f, 1f)] private float startScale = 0.7f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Tween currentAnimation; // 用于追踪当前动画，防止冲突

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 播放入场动画 (淡入 + 缩放弹出)
    /// </summary>
    public void PlayShowAnimation()
    {
        // 如果有正在进行的动画，先中断它
        currentAnimation?.Kill();

        // 设置初始状态
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * startScale;
        gameObject.SetActive(true);

        // 创建动画序列
        Sequence showSeq = DOTween.Sequence();

        // 同时执行淡入和缩放到1
        showSeq.Join(canvasGroup.DOFade(1f, animationDuration));
        showSeq.Join(rectTransform.DOScale(1f, animationDuration).SetEase(showEase));

        // 保存当前动画引用
        currentAnimation = showSeq;
    }

    /// <summary>
    /// 播放退场动画 (淡出 + 缩小)，完成后自动隐藏面板
    /// </summary>
    public void PlayHideAnimation()
    {
        currentAnimation?.Kill();

        Sequence hideSeq = DOTween.Sequence();

        hideSeq.Join(canvasGroup.DOFade(0f, animationDuration));
        hideSeq.Join(rectTransform.DOScale(startScale, animationDuration).SetEase(hideEase));

        // 动画完成后，将面板设置为不激活状态
        hideSeq.OnComplete(() => gameObject.SetActive(false));

        currentAnimation = hideSeq;
    }

    private void OnDestroy()
    {
        // 对象销毁时，确保对应的动画也被杀死，防止报错
        currentAnimation?.Kill();
    }
}