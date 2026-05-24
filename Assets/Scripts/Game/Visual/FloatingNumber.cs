using TMPro;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 浮字组件：控制单个浮字的显示和动画
/// </summary>
public class FloatingNumber : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private TextMeshProUGUI text;       // 数字文本
    [SerializeField] private CanvasGroup canvasGroup;   // 用于淡出

    [Header("普通样式（物理/魔法/治疗）")]
    [SerializeField] private float normalPulseScale = 1.3f;      // 合并时的脉冲放大倍率
    [SerializeField] private float normalPulseDuration = 0.15f;   // 脉冲动画时长
    [SerializeField] private float normalFloatHeight = 1.0f;      // 上飘高度
    [SerializeField] private float normalFloatDuration = 0.5f;     // 上飘时长
    [SerializeField] private float normalFinalScale = 1.2f;        // 飘走结束时的大小
    [SerializeField] private float normalFontSize = 1f;            // 字体大小

    [Header("特殊伤害样式（向下飘，不参与排序）")]
    [SerializeField] private Color specialColor = new Color(0.6f, 0.2f, 0.2f); // 暗红色
    [SerializeField] private float specialFontSize = 0.8f;          // 较小字体
    [SerializeField] private float specialDropHeight = -0.5f;       // 向下飘落的高度（负值）
    [SerializeField] private float specialDropDuration = 0.8f;      // 飘落动画时长
    [SerializeField] private float specialFinalScale = 0.8f;        // 结束时缩放（略微缩小）
    [SerializeField] private float specialInitialYOffset = -0.2f;   // 初始位置比正常低多少

    // 当前动画参数（由初始化方法设置）
    private float currentMoveHeight;    // 移动高度（正上飘，负下飘）
    private float currentMoveDuration;  // 移动时长
    private float currentFinalScale;    // 结束缩放

    public bool IsFloatingAway { get; private set; }  // 是否正在飘走

    private Tween currentTween;                     // 当前动画
    private System.Action onFloatComplete;          // 飘走完成回调

    // 普通类型的颜色常量
    private static readonly Color PhysicalColor = new Color(1f, 0.85f, 0.2f);  // 金黄
    private static readonly Color MagicalColor = new Color(0.8f, 0.4f, 1f);     // 紫罗兰
    private static readonly Color HealingColor = new Color(0.3f, 0.9f, 0.4f);   // 翠绿

    /// <summary>
    /// 普通初始化（物理/魔法/治疗），向上飘
    /// </summary>
    public void Initialize(Vector3 worldPos, int value, FloatingNumberType type)
    {
        transform.position = worldPos;
        SetValue(value, type);
        IsFloatingAway = false;
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;

        // 应用普通样式参数
        currentMoveHeight = normalFloatHeight;
        currentMoveDuration = normalFloatDuration;
        currentFinalScale = normalFinalScale;
        text.fontSize = normalFontSize;

        // 初始脉冲动画
        PlayMergePulse();
    }

    /// <summary>
    /// 特殊伤害初始化（暗红色、小字体、向下飘、初始位置更低）
    /// </summary>
    /// <param name="worldPos">基础世界坐标（格子中心+固定偏移）</param>
    /// <param name="value">伤害数值</param>
    public void InitializeAsSpecial(Vector3 worldPos, int value)
    {
        // 应用初始Y偏移（更低的位置）
        Vector3 startPos = worldPos + Vector3.up * specialInitialYOffset;
        transform.position = startPos;

        text.text = value.ToString();
        text.color = specialColor;
        text.fontSize = specialFontSize;
        IsFloatingAway = false;
        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
        currentTween?.Kill();

        // 应用特殊伤害样式参数
        currentMoveHeight = specialDropHeight;   // 负值表示向下移动
        currentMoveDuration = specialDropDuration;
        currentFinalScale = specialFinalScale;
    }

    /// <summary>
    /// 更新数值和颜色（用于合并时）
    /// </summary>
    public void SetValue(int value, FloatingNumberType type)
    {
        text.text = value.ToString();
        text.color = GetColor(type);
    }

    private Color GetColor(FloatingNumberType type)
    {
        switch (type)
        {
            case FloatingNumberType.Physical: return PhysicalColor;
            case FloatingNumberType.Magical:  return MagicalColor;
            case FloatingNumberType.Healing:  return HealingColor;
            default: return Color.white;
        }
    }

    /// <summary>
    /// 播放合并脉冲动画（短暂放大再缩回）
    /// </summary>
    public void PlayMergePulse()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(normalPulseScale, normalPulseDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => transform.DOScale(1f, normalPulseDuration).SetEase(Ease.InBack));
    }

    /// <summary>
    /// 播放飘走/飘落动画（根据 isMovingDown 自动选择方向）
    /// </summary>
    public void PlayFloatAway(System.Action onComplete)
    {
        IsFloatingAway = true;
        onFloatComplete = onComplete;

        Sequence seq = DOTween.Sequence();
        // 移动：向上为正偏移，向下为负偏移
        seq.Join(transform.DOMoveY(transform.position.y + currentMoveHeight, currentMoveDuration).SetEase(Ease.OutQuad));
        seq.Join(canvasGroup.DOFade(0f, currentMoveDuration));
        seq.Join(transform.DOScale(currentFinalScale, currentMoveDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() => onFloatComplete?.Invoke());

        currentTween = seq;
    }

    /// <summary>
    /// 强制停止动画并清理回调（回收前调用）
    /// </summary>
    public void ForceRecycle()
    {
        currentTween?.Kill();
        onFloatComplete = null;
    }
}