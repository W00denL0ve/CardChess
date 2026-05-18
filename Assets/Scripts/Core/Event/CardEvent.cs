/// <summary>
/// 卡牌打出事件 — 玩家点击手牌中的卡牌时触发
/// </summary>
public class CardPlayedEvent : GameEvent
{
    public CardData Card { get; }
    public CardPlayedEvent(CardData card) => Card = card;
}
