using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;

/// <summary>
/// 手牌区域管理器 — 池化管理 CardVisualizer，数据与表现分离
/// </summary>
public class HandUI : MonoBehaviour
{
    public static HandUI Instance { get; private set; }

    [Header("预制体")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject handCanvasPrefab;

    [Header("运行时引用")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform pendingArea;

    [Header("按钮名称（自动查找）")]
    [SerializeField] private string endTurnButtonName = "EndTurnButton";
    [SerializeField] private string drawPileButtonName = "DrawPile";
    [SerializeField] private string discardDeckButtonName = "DiscardDeck";
    [SerializeField] private string consumedDeckButtonName = "ConsumedDeck";
    [SerializeField] private string energyDisplayName = "EnergyDisplay";


    [Header("布局")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float cardSpacing = 120f;
    [SerializeField] private float fanAngle = 15f;
    [SerializeField] private float arcDrop = 20f;
    [SerializeField] private float arcPeak = 30f;
    [SerializeField] private float marginBase = 0.25f;
    [SerializeField] private float marginShrinkPerCard = 0.04f;
    [SerializeField] private float hoverHeight = 70f;
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float pushDistance = 40f;
    [SerializeField] private float layoutAnimDuration = 0.2f;
    [Header("交互")]
    [SerializeField, Tooltip("左")] private float raycastPadLeft = 0f;
    [SerializeField, Tooltip("下")] private float raycastPadBottom = 0f;
    [SerializeField, Tooltip("右")] private float raycastPadRight = 0f;
    [SerializeField, Tooltip("上")] private float raycastPadTop = 0f;
    private Queue<CardVisualizer> pool = new();
    private List<CardVisualizer> activeCards = new();
    private Dictionary<CardData, System.Action> pendingArrivalCallbacks = new();
    private UnityEngine.UI.Button endTurnButton;
    private UnityEngine.UI.Button drawPileButton;
    private UnityEngine.UI.Button discardDeckButton;
    private TextMeshProUGUI drawPileCardLeftDisplay;
    private TextMeshProUGUI energyDisplayTMP;
    private Transform canvasTransform;
    private int hoveredIndex = -1;
    private bool hoverEnabled = false;
    private CardVisualizer _clickedCard;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 实例化手牌 Canvas 预制体
        GameObject canvasGO = null;
        if (handCanvasPrefab != null)
        {
            canvasGO = Instantiate(handCanvasPrefab);
            canvasGO.name = "HandCanvas";
            canvasTransform = canvasGO.transform;

            // 从预制体中查找容器
            if (handContainer == null)
            {
                var found = canvasGO.transform.Find("HandArea");
                if (found != null) handContainer = found;
            }
            if (pendingArea == null)
            {
                var found = canvasGO.transform.Find("PendingArea");
                if (found != null) pendingArea = found;
            }

            // 按名称查找按钮
            endTurnButton = FindButton(canvasGO.transform, endTurnButtonName);
            drawPileButton = FindButton(canvasGO.transform, drawPileButtonName);
            discardDeckButton = FindButton(canvasGO.transform, discardDeckButtonName);

            // Logger.Log($"drawPileButton位置为{drawPileButton.transform.position}");
            // Logger.Log($"discardDeckButton位置为{discardDeckButton.transform.position}");

            // 获取TMP显示
            drawPileCardLeftDisplay = drawPileButton.transform.GetComponentInChildren<TextMeshProUGUI>();

            // 能量显示
            var energyBtn = FindButton(canvasGO.transform, energyDisplayName);
            if (energyBtn != null)
            {
                energyDisplayTMP = energyBtn.transform.GetComponentInChildren<TextMeshProUGUI>();
                energyDisplayTMP.text = ResourceManager.Instance?.Energy.ToString() ?? "0";
            }
        }

        if (handContainer == null)
        {
            Logger.LogError("[HandUI] handContainer 为空，请检查 HandCanvas 预制体");
            return;
        }

        // 预实例化池
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(cardPrefab, handContainer);
            go.SetActive(false);
            pool.Enqueue(go.GetComponent<CardVisualizer>());
        }

        // 订阅事件
        GameEventChannel.Register<CardDrawnEvent>(OnCardDrawn);
        GameEventChannel.Register<CardPlayedEvent>(OnCardPlayed);
        GameEventChannel.Register<PhaseChangedEvent>(OnPhaseChanged);
        GameEventChannel.Register<ResourceChangedEvent>(OnResourceChanged);

        // 结束回合按钮
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<CardDrawnEvent>(OnCardDrawn);
        GameEventChannel.Unregister<CardPlayedEvent>(OnCardPlayed);
        GameEventChannel.Unregister<PhaseChangedEvent>(OnPhaseChanged);
        GameEventChannel.Unregister<ResourceChangedEvent>(OnResourceChanged);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    /// <summary>绑定一张卡牌到手牌区</summary>
    public void AddCard(CardData cardData)
    {
        var cv = GetFromPool();
        cv.Bind(cardData, activeCards.Count);
        activeCards.Add(cv);
        RefreshLayout();
        RefreshCardCostColors();
    }

    /// <summary>从手牌区移除一张卡牌并回收到池</summary>
    public void RemoveCard(CardData cardData, Action onComplete = null)
    {
        var cv = activeCards.Find(c => c.cardData == cardData);
        if (cv != null)
        {
            activeCards.Remove(cv);
            cv.gameObject.SetActive(false);
            pool.Enqueue(cv);
        }
        RefreshLayout();
        onComplete?.Invoke();
    }

    /// <summary>清空手牌（如关卡结束）</summary>
    public void Clear()
    {
        foreach (var cv in activeCards)
        {
            cv.gameObject.SetActive(false);
            pool.Enqueue(cv);
        }
        activeCards.Clear();
    }
 
    /// <summary>
    /// 更新剩余牌数显示
    /// </summary>
    /// <param name="num_left"></param>
    public void UpdateDrawPileDisplay(int num_left)
    {
        if (drawPileCardLeftDisplay == null) return;
        // 数量减少时加一个短暂的数字滚动
        int current = int.TryParse(drawPileCardLeftDisplay.text, out int c) ? c : num_left;
        if (current != num_left)
        {
            float val = current;
            DOTween.To(() => val, x => { val = x; drawPileCardLeftDisplay.text = Mathf.RoundToInt(val).ToString(); }, num_left, 0.2f);
        }
        else
        {
            drawPileCardLeftDisplay.text = num_left.ToString();
        }
    }

    /// <summary>牌库数量滚动动画（弃牌堆洗回时调用）</summary>
    public IEnumerator PlayDeckCountAnimation(int from, int to, float duration = 0.5f)
    {
        if (drawPileCardLeftDisplay == null) yield break;
        float val = from;
        Tween t = DOTween.To(() => val, x => { val = x; drawPileCardLeftDisplay.text = Mathf.RoundToInt(val).ToString(); }, to, duration);
        AudioManager.Instance.PlaySound("cardShuffle");
        yield return t.WaitForCompletion();
    }

    // ═══ 悬停 ═══

    private void OnPhaseChanged(PhaseChangedEvent evt)
    {
        hoverEnabled = evt.newPhase == TurnPhase.PlayerPlay;
        if (!hoverEnabled && hoveredIndex >= 0)
        {
            hoveredIndex = -1;
            AnimateLayout();
        }
    }

    private void OnResourceChanged(ResourceChangedEvent evt)
    {
        if (evt.type != ResourceType.Energy || energyDisplayTMP == null) return;

        // DOTween 数字滚动：从旧值过渡到新值
        DOTween.Kill(energyDisplayTMP); // 防止动画重叠
        float val = evt.oldValue;
        DOTween.To(() => val, x => { val = x; energyDisplayTMP.text = Mathf.RoundToInt(val).ToString(); }, evt.newValue, 0.25f)
               .SetTarget(energyDisplayTMP)
               .SetEase(Ease.OutQuad);

        // 同步更新所有手牌卡牌的费用颜色
        RefreshCardCostColors();
    }

    /// <summary>根据当前能量更新所有手牌卡牌的费用颜色和发光状态</summary>
    private void RefreshCardCostColors()
    {
        int currentEnergy = ResourceManager.Instance?.Energy ?? 0;
        foreach (var cv in activeCards)
        {
            bool affordable = cv.cardData != null && currentEnergy >= cv.cardData.Cost;
            cv.AnimateCostColor(affordable ? cv.affordableColor : cv.unaffordableColor);
            cv.SetGlowEnabled(affordable);
        }
    }

    public void OnCardHovered(int index)
    {
        if (!hoverEnabled) return;
        if (index < 0 || index >= activeCards.Count) return;
        hoveredIndex = index;
        AudioManager.Instance.PlaySound("cardFlip");
        AnimateLayout();
    }

    public void OnCardUnhovered()
    {
        if (hoveredIndex < 0) return;
        hoveredIndex = -1;
        AnimateLayout();
    }

    /// <summary>平滑过渡到目标布局（DOTween）</summary>
    private void AnimateLayout()
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            var (pos, angle) = GetCardPosition(i, activeCards.Count);
            ApplyHoverOffset(i, ref pos);

            var cv = activeCards[i];
            cv.SetHandIndex(i);
            cv.transform.DOLocalMove(pos, layoutAnimDuration).SetEase(Ease.OutQuad);
            cv.transform.DOLocalRotate(new Vector3(0, 0, angle), layoutAnimDuration).SetEase(Ease.OutQuad);
            cv.transform.DOScale(i == hoveredIndex ? cv.BaseScale * hoverScale : cv.BaseScale, layoutAnimDuration).SetEase(Ease.OutQuad);

            if (i == hoveredIndex)
                cv.transform.SetAsLastSibling();
            else
                cv.transform.SetSiblingIndex(i);
        }
    }

    /// <summary>即时刷新布局（无动画）</summary>
    public void RefreshLayout()
    {
        hoveredIndex = -1;

        for (int i = 0; i < activeCards.Count; i++)
        {
            var (pos, angle) = GetCardPosition(i, activeCards.Count);
            ApplyHoverOffset(i, ref pos);

            var cv = activeCards[i];
            cv.SetHandIndex(i);
            cv.transform.localPosition = pos;
            cv.transform.localRotation = Quaternion.Euler(0, 0, angle);
            cv.transform.localScale = i == hoveredIndex ? cv.BaseScale * hoverScale : cv.BaseScale;

            if (i == hoveredIndex)
                cv.transform.SetAsLastSibling();
            else
                cv.transform.SetSiblingIndex(i);
        }
    }

    /// <summary>应用悬停时的偏移：上移 + 相邻牌挤开</summary>
    private void ApplyHoverOffset(int index, ref Vector3 pos)
    {
        if (hoveredIndex < 0) return;

        if (index == hoveredIndex)
        {
            pos.y += hoverHeight;
        }
        else
        {
            int dist = Mathf.Abs(index - hoveredIndex);
            float push = Mathf.Lerp(pushDistance, 0f, Mathf.Min(dist, 3) / 3f);
            pos.x += (index < hoveredIndex ? -1 : 1) * push;
        }
    }

    // ═══ 布局计算 ═══

    /// <summary>计算某张卡牌在弧线布局中的位置和旋转</summary>
    private (Vector3 pos, float angle) GetCardPosition(int index, int totalCount)
    {
        float margin = Mathf.Max(0, marginBase - totalCount * marginShrinkPerCard);
        float t = totalCount > 1
            ? Mathf.Lerp(margin, 1f - margin, (float)index / (totalCount - 1))
            : 0.5f;

        float halfSpan = (totalCount - 1) * cardSpacing * 0.5f;
        float x = Mathf.Lerp(-halfSpan, halfSpan, t);
        float y = arcPeak * (1 - Mathf.Pow(2 * t - 1, 2)) - arcDrop;

        Vector3 pos = new Vector3(x, y, 0);
        float angle = Mathf.Lerp(fanAngle, -fanAngle, t);
        return (pos, angle);
    }

    // ═══ 抽牌动画待处理列表 ═══

    private List<CardVisualizer> pendingDrawCards = new();

    void OnCardDrawn(CardDrawnEvent evt)
    {
        var cv = GetFromPool();
        cv.Bind(evt.Card);
        cv.gameObject.SetActive(false);
        pendingDrawCards.Add(cv);
    }

    /// <summary>异步群组抽牌动画 — 卡牌从牌库位置逐张飞入手牌</summary>
    public IEnumerator PlayGroupDrawAnimation(float cardDelay = 0.08f, float flyDuration = 0.3f)
    {
        if (pendingDrawCards.Count == 0) yield break;

        Vector3 startPos = drawPileButton != null
            ? drawPileButton.transform.position
            : Vector3.zero;
        Quaternion drawStartRot = Quaternion.Euler(-90f, 0f, 90f);

        // 先将所有卡牌加入 activeCards，一次性布局
        foreach (var cv in pendingDrawCards)
        {
            cv.gameObject.SetActive(true);
            activeCards.Add(cv);
        }
        RefreshLayout();

        // 记录目标位置和曲线旋转后，全部移回起点（避免闪现和正面显示）
        Vector3[] targets = new Vector3[pendingDrawCards.Count];
        Quaternion[] targetRots = new Quaternion[pendingDrawCards.Count];
        for (int i = 0; i < pendingDrawCards.Count; i++)
        {
            var cv = pendingDrawCards[i];
            targets[i] = cv.transform.position;
            targetRots[i] = cv.transform.rotation;
            cv.transform.position = startPos;
            cv.transform.rotation = drawStartRot;
        }

        for (int i = 0; i < pendingDrawCards.Count; i++)
        {
            StartCoroutine(pendingDrawCards[i].PlayDrawAnimation(startPos, targets[i], flyDuration, drawStartRot, targetRots[i]));
            yield return new WaitForSeconds(cardDelay);
        }

        yield return new WaitForSeconds(flyDuration);

        // 动画完成后恢复扇形布局（旋转被动画覆盖了）
        RefreshLayout();
        RefreshCardCostColors();
        pendingDrawCards.Clear();
    }

    // ═══ 弃牌动画 ═══

    private List<CardVisualizer> pendingDiscardCards = new();

    /// <summary>将指定的卡牌标记为待弃牌</summary>
    public void MarkCardsForDiscard(List<CardData> cards)
    {
        foreach (var card in cards)
        {
            var cv = activeCards.Find(c => c.cardData == card);
            if (cv != null)
            {
                activeCards.Remove(cv);
                pendingDiscardCards.Add(cv);
            }
        }
    }

    /// <summary>弃牌动画 — 卡牌从手牌飞入弃牌堆</summary>
    public IEnumerator PlayDiscardAnimation(float cardDelay = 0.06f, float flyDuration = 0.25f)
    {
        if (pendingDiscardCards.Count == 0) yield break;

        Vector3 targetPos = discardDeckButton != null
            ? discardDeckButton.transform.position
            : Vector3.zero;
        Quaternion discardEndRot = Quaternion.Euler(-90f, 0f, 90f);

        RefreshLayout();

        for (int i = 0; i < pendingDiscardCards.Count; i++)
        {
            var cv = pendingDiscardCards[i];
            Vector3 startPos = cv.transform.position;

            StartCoroutine(cv.PlayDrawAnimation(startPos, targetPos, flyDuration, null, discardEndRot));
            yield return new WaitForSeconds(cardDelay);
        }

        yield return new WaitForSeconds(flyDuration);

        // 回收所有弃牌视觉
        foreach (var cv in pendingDiscardCards)
        {
            cv.gameObject.SetActive(false);
            pool.Enqueue(cv);
        }
        pendingDiscardCards.Clear();
        RefreshLayout();
    }

    // ═══ 池化管理 ═══

    CardVisualizer GetFromPool()
    {
        Vector4 pad = new Vector4(raycastPadLeft, raycastPadBottom, raycastPadRight, raycastPadTop);
        if (pool.Count > 0)
        {
            var cv = pool.Dequeue();
            cv.transform.SetParent(handContainer, false);
            // 重置交互状态（可能被 Pending 时禁用了）
            var images = cv.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                img.raycastTarget = true;
                img.raycastPadding = pad;
            }
            cv.SetGlowEnabled(false);
            cv.gameObject.SetActive(true);
            return cv;
        }
        var go = Instantiate(cardPrefab, handContainer);
        var newCv = go.GetComponent<CardVisualizer>();
        var newImages = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in newImages) img.raycastPadding = pad;
        return newCv;
    }

    void OnEndTurnClicked()
    {
        GameEventChannel.Dispatch(new EndPlayerTurnEvent());
    }

    /// <summary>在预制体层级中按名称查找按钮（递归）</summary>
    private UnityEngine.UI.Button FindButton(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;

        // 先检查当前节点
        var btn = root.GetComponent<UnityEngine.UI.Button>();
        if (btn != null && root.name == name) return btn;

        // 递归子节点
        for (int i = 0; i < root.childCount; i++)
        {
            var result = FindButton(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    public void OnCardClicked(CardVisualizer cv) => _clickedCard = cv;

    /// <summary>注册卡牌到达 pending 区后的回调</summary>
    public void WaitForPendingArrival(CardData card, System.Action onArrived)
    {
        pendingArrivalCallbacks[card] = onArrived;
    }

    void OnCardPlayed(CardPlayedEvent evt)
    {
        // 消费缓存，立即清空（消费即清理，绝不留给下一帧）
        CardVisualizer cv = _clickedCard;
        _clickedCard = null;

        // 双重校验：缓存与事件卡牌一致；不一致时 fallback 到 Find
        if (cv == null || cv.cardData != evt.Card)
        {
            cv = activeCards.Find(c => c.cardData == evt.Card);
        }
        if (cv == null) return;

        // 终止残留的布局动画（悬停回位等），确保起始位置准确
        cv.transform.DOKill();
        cv.SetGlowEnabled(false);

        // 从手牌移除
        activeCards.Remove(cv);
        cv.SetHandIndex(-1);
        var images = cv.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images) img.raycastTarget = false;
        RefreshLayout();

        // 视觉听觉效果
        AudioManager.Instance.PlaySound("cardPlay");
        StartCoroutine(AnimateCardToPending(cv));
    }

    private IEnumerator AnimateCardToPending(CardVisualizer cv, float duration = 0.3f)
    {
        // 以 Canvas 中心作为飞入中点
        Vector3 centerPos = canvasTransform.position;
        if (canvasTransform is RectTransform rt)
            centerPos = canvasTransform.TransformPoint(rt.rect.center);

        // 阶段一：飞往屏幕中央 → 放大停留
        yield return cv.PlayDrawAnimation(cv.transform.position, centerPos, duration * 0.6f);
        cv.transform.DOScale(cv.BaseScale * hoverScale, 0.12f).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.2f);

        // 阶段二：从中央飞往 pending 区 → 恢复大小
        cv.transform.SetParent(pendingArea, true);
        Vector3 pendingPos = pendingArea != null ? pendingArea.position : Vector3.zero;
        float randomZ = UnityEngine.Random.Range(-20f, 20f);
        Quaternion pendingRot = Quaternion.Euler(0f, 0f, randomZ);
        cv.transform.DOScale(cv.BaseScale, 0.12f).SetEase(Ease.OutQuad);
        yield return cv.PlayDrawAnimation(cv.transform.position, pendingPos, duration * 0.4f, null, pendingRot);

        // 通知等待者卡牌已到达 pending 区
        if (cv.cardData != null && pendingArrivalCallbacks.TryGetValue(cv.cardData, out var cb))
        {
            pendingArrivalCallbacks.Remove(cv.cardData);
            cb?.Invoke();
        }
    }

    /// <summary>效果链完成后，将卡牌从待命区动画到去向位置</summary>
    public IEnumerator AnimateCardToDestination(CardData card, float duration = 0.3f)
    {
        var all = pendingArea.GetComponentsInChildren<CardVisualizer>(false);
        CardVisualizer cv = null;
        foreach (var c in all)
        {
            if (c.cardData == card) { cv = c; break; }
        }
        if (cv == null)
        {
            Logger.LogWarning($"[HandUI] AnimateCardToDestination: 未找到 {card.cardName} 的 CardVisualizer（pendingArea 下共 {all.Length} 个）");
            yield break;
        }

        Vector3 targetPos;
        switch (card.destination)
        {
            case DestinationOnPlay.Destroy:
                var consumedBtn = FindButtonInChildren(consumedDeckButtonName);
                targetPos = consumedBtn != null ? consumedBtn.transform.position : Vector3.zero;
                break;
            case DestinationOnPlay.ReturnToHand:
                targetPos = handContainer.position;
                break;
            case DestinationOnPlay.Discard:
            default:
                targetPos = discardDeckButton != null ? discardDeckButton.transform.position : Vector3.zero;
                break;
        }

        Quaternion deckRot = Quaternion.Euler(-90f, 0f, 90f);

        yield return cv.PlayDrawAnimation(cv.transform.position, targetPos, duration, null,
            card.destination == DestinationOnPlay.ReturnToHand ? Quaternion.identity : deckRot);

        if (card.destination == DestinationOnPlay.ReturnToHand)
        {
            // 回到手牌：重新加入布局
            cv.transform.SetParent(handContainer, true);
            var images = cv.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images) img.raycastTarget = true;
            activeCards.Add(cv);
            RefreshLayout();
            RefreshCardCostColors();
        }
        else
        {
            // 弃牌/销毁：回收视觉
            cv.SetGlowEnabled(false);
            cv.gameObject.SetActive(false);
            pool.Enqueue(cv);
        }
    }

    private UnityEngine.UI.Button FindButtonInChildren(string name)
    {
        return FindButton(canvasTransform, name);
    }
}