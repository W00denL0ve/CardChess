using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 卡牌视觉表现 — 纯 UI 层
/// 不包含任何效果执行逻辑
/// </summary>
public class CardVisualizer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;
    public Image artworkImage;
    public TextMeshProUGUI nameTMP;
    public TextMeshProUGUI descriptionTMP;
    public TextMeshProUGUI costTMP;
    public Image glowOverlay;

    [Header("能量颜色")]
    public Color affordableColor = Color.white;
    public Color unaffordableColor = Color.red;

    public Vector3 BaseScale { get; private set; }

    private int handIndex;

    void Awake()
    {
        BaseScale = transform.localScale;
        if (glowOverlay != null)
        {
            var c = glowOverlay.color;
            c.a = 0f;
            glowOverlay.color = c;
            glowOverlay.gameObject.SetActive(true);
        }
    }

    public void Bind(CardData data, int index = -1)
    {
        cardData = data;
        handIndex = index;
        RefreshUI();
    }

    public void SetHandIndex(int index) => handIndex = index;

    public void RefreshUI()
    {
        if (cardData == null) return;
        if (artworkImage != null) artworkImage.sprite = cardData.artwork;
        if (nameTMP != null) nameTMP.text = cardData.cardName;
        if (descriptionTMP != null) descriptionTMP.text = cardData.description;
        SetCostText(cardData.Cost);
    }

    /// <summary>设置能量消耗数字</summary>
    public void SetCostText(int cost)
    {
        if (costTMP == null) return;
        costTMP.text = cost.ToString();
    }

    /// <summary>动画过渡到目标颜色</summary>
    public void AnimateCostColor(Color target, float duration = 0.15f)
    {
        if (costTMP == null) return;
        DOTween.Kill(costTMP);
        costTMP.DOColor(target, duration).SetEase(Ease.OutQuad);
    }

    /// <summary>根据是否可打出切换发光状态（常亮 / 关闭）</summary>
    public void SetGlowEnabled(bool enabled)
    {
        if (glowOverlay == null) return;
        DOTween.Kill(glowOverlay);
        if (enabled)
        {
            glowOverlay.DOFade(1f, 0.25f).SetEase(Ease.OutQuad);
        }
        else
        {
            glowOverlay.DOFade(0f, 0.15f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Logger.Log("鼠标点击");
        if (cardData == null) return;
        // 先缓存自身引用，供 HandUI.OnCardPlayed 精确取到（避免同名卡 Find 歧义）
        HandUI.Instance?.OnCardClicked(this);
        GameEventChannel.Dispatch(new CardClickedEvent(cardData));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Logger.Log("鼠标悬停");
        HandUI.Instance?.OnCardHovered(handIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HandUI.Instance?.OnCardUnhovered();
    }

    /// <summary>从起始位置飞到目标位置，可指定起始/结束旋转</summary>
    public IEnumerator PlayDrawAnimation(Vector3 from, Vector3 to, float duration,
        Quaternion? fromRot = null, Quaternion? toRot = null)
    {
        Quaternion startRot = fromRot ?? transform.rotation;
        Quaternion endRot = toRot ?? transform.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(from, to, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        transform.position = to;
        transform.rotation = endRot;
    }
}