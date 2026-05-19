using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手牌区域管理器 — 池化管理 CardVisualizer，数据与表现分离
/// </summary>
public class HandUI : MonoBehaviour
{
    [Header("预制体")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject handCanvasPrefab;

    [Header("运行时引用")]
    [SerializeField] private Transform handContainer;

    [Header("按钮名称（自动查找）")]
    [SerializeField] private string endTurnButtonName = "EndTurnButton";

    [Header("布局")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float cardSpacing = 100f;

    private Queue<CardVisualizer> pool = new();
    private List<CardVisualizer> activeCards = new();
    private UnityEngine.UI.Button endTurnButton;

    void Start()
    {
        // 实例化手牌 Canvas 预制体
        GameObject canvasGO = null;
        if (handCanvasPrefab != null)
        {
            canvasGO = Instantiate(handCanvasPrefab);
            canvasGO.name = "HandCanvas";

            // 从预制体中查找容器
            if (handContainer == null)
            {
                var found = canvasGO.transform.Find("HandArea");
                if (found != null) handContainer = found;
            }

            // 按名称查找按钮
            endTurnButton = FindButton(canvasGO.transform, endTurnButtonName);
        }

        if (handContainer == null)
        {
            Debug.LogError("[HandUI] handContainer 为空，请检查 HandCanvas 预制体");
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

        // 结束回合按钮
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<CardDrawnEvent>(OnCardDrawn);
        GameEventChannel.Unregister<CardPlayedEvent>(OnCardPlayed);

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    /// <summary>绑定一张卡牌到手牌区</summary>
    public void AddCard(CardData cardData)
    {
        var cv = GetFromPool();
        cv.Bind(cardData);
        activeCards.Add(cv);
        ReLayout();
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
        ReLayout();
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

    // ═══ 内部 ═══

    CardVisualizer GetFromPool()
    {
        if (pool.Count > 0)
        {
            var cv = pool.Dequeue();
            cv.gameObject.SetActive(true);
            return cv;
        }
        var go = Instantiate(cardPrefab, handContainer);
        return go.GetComponent<CardVisualizer>();
    }

    void ReLayout()
    {
        int count = activeCards.Count;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * cardSpacing;
            activeCards[i].transform.localPosition = new Vector3(x, 0, 0);
        }
    }

    void OnCardDrawn(CardDrawnEvent evt)
    {
        AddCard(evt.Card);
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

    void OnCardPlayed(CardPlayedEvent evt)
    {
        // 卡牌已不在手牌，由 DeckManager 处理数据
        // 此处仅移除视觉效果
        RemoveCard(evt.Card);
    }
}