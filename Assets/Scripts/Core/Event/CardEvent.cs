/// <summary>
/// 卡牌点击事件 — 玩家点击了手牌中的卡牌（仅 UI 通知，不代表卡牌已被打出）
/// </summary>
public class CardClickedEvent : GameEvent
{
    public CardData Card { get; }
    public CardClickedEvent(CardData card) => Card = card;
}

/// <summary>
/// 卡牌打出事件 — 卡牌已从手牌移除，进入 pending 状态
/// </summary>
public class CardPlayedEvent : GameEvent
{
    public CardData Card { get; }
    public CardPlayedEvent(CardData card) => Card = card;
}

/// <summary>
/// 卡牌抽到事件 — 卡牌从牌库进入手牌时触发
/// </summary>
public class CardDrawnEvent : GameEvent
{
    public CardData Card { get; }
    public CardDrawnEvent(CardData card) => Card = card;
}
