using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手牌区域管理器 — 池化管理 CardVisualizer，数据与表现分离
/// </summary>
public class HandUI : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handContainer;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float cardSpacing = 100f;

    private Queue<CardVisualizer> pool = new();
    private List<CardVisualizer> activeCards = new(); // 当前手牌中显示的卡牌

    void Start()
    {
        // 预实例化池
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(cardPrefab, handContainer);
            go.SetActive(false);
            pool.Enqueue(go.GetComponent<CardVisualizer>());
        }

        // 订阅事件
        GameEventChannel.Register<CardPlayedEvent>(OnCardPlayed);
    }

    void OnDestroy()
    {
        GameEventChannel.Unregister<CardPlayedEvent>(OnCardPlayed);
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

    void OnCardPlayed(CardPlayedEvent evt)
    {
        // 卡牌已不在手牌，由 DeckManager 处理数据
        // 此处仅移除视觉效果
        RemoveCard(evt.Card);
    }
}