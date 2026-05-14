using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 独立控制镂空遮罩（UI/GuideMask Shader）半径动画的组件。
/// </summary>
public class MaskRadiusAnimator : MonoBehaviour
{
    private const float DEFAULT_START_RADIUS = 0.4f;
    private const float DEFAULT_END_RADIUS = 0.1f;
    private const float DEFAULT_DURATION = 0.5f;

    [Header("目标材质")]
    [Tooltip("使用 UI/GuideMask 着色器的材质实例")]
    public Material targetMaterial;

    [Header("动画参数")]
    [Tooltip("起始半径 (UV空间, 0~1)")]
    [Range(0f, 1f)]
    public float startRadius = 0.4f;

    [Tooltip("结束半径 (UV空间, 0~1)")]
    [Range(0f, 1f)]
    public float endRadius = 0.1f;

    [Tooltip("动画时长（秒）")]
    public float duration = 0.5f;

    [Tooltip("动画曲线（可调整动态节奏）")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("自动播放")]
    [Tooltip("在 Start 时自动播放动画")]
    public bool playOnStart = true;

    private Coroutine animationCoroutine;

    [Header("边缘模式")]
    [Tooltip("勾选后自动将材质设为硬边缘（Softness = 0）")]
    public bool forceHardEdge = true;   // 强制硬边缘开关

    private void Awake()
    {
        if (targetMaterial == null)
        {
            // 尝试从自身 Image 组件获取材质
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null)
                targetMaterial = image.material;
            else
                Debug.LogWarning("MaskRadiusAnimator：未指定目标材质，也未找到 Image 组件。");
        }

        SetRadius(startRadius); // 初始化半径

    }

    private void OnEnable()
    {
        // 每次激活时，确保自己在 Canvas 的最上层
        transform.SetAsLastSibling();
    }

    private void Start()
    {
        // 强制硬边缘
        if (forceHardEdge && targetMaterial != null)
        {
            targetMaterial.SetFloat("_MaskSoftness", 0f);
        }

        if (playOnStart && targetMaterial != null)
            PlayAnimation();

        if (playOnStart && targetMaterial != null)
        {
            PlayAnimation();
        }
    }

    public void SetParameters(float startRadius = DEFAULT_START_RADIUS, float endRadius = DEFAULT_END_RADIUS, float time = DEFAULT_DURATION)
    {
        this.startRadius = startRadius;
        this.endRadius = endRadius;
        duration = time;
    }

    /// <summary>
    /// 播放半径动画。
    /// </summary>
    public void PlayAnimation()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("MaskRadiusAnimator：targetMaterial 为空，无法播放动画。");
            return;
        }

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        // Debug.Log($"MaskRadiusAnimator：开始播放动画，startRadius={startRadius}, endRadius={endRadius}, duration={duration}");
        animationCoroutine = StartCoroutine(AnimateRadius(startRadius, endRadius, duration));
    }

    /// <summary>
    /// 反向播放动画（从 endRadius 到 startRadius）。
    /// </summary>
    public void PlayAnimationReverse()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("MaskRadiusAnimator：targetMaterial 为空，无法播放动画。");
            return;
        }

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateRadius(endRadius, startRadius, duration));
    }

    /// <summary>
    /// 停止当前正在播放的动画。
    /// </summary>
    public void StopAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    /// <summary>
    /// 立即设置半径（无动画）。
    /// </summary>
    public void SetRadius(float radius)
    {
        if (targetMaterial != null)
            targetMaterial.SetFloat("_MaskRadius", radius);
    }

    private IEnumerator AnimateRadius(float from, float to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / time);
            // 应用动画曲线
            float curvedT = easingCurve.Evaluate(t);
            float currentRadius = Mathf.Lerp(from, to, curvedT);
            targetMaterial.SetFloat("_MaskRadius", currentRadius);
            yield return null;
        }
        targetMaterial.SetFloat("_MaskRadius", to);
        animationCoroutine = null;
    }
}