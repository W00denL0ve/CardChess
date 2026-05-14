using UnityEngine;

/// <summary>
/// 卡牌目标 - 包装一张卡牌（例如效果可以修改手牌）
/// </summary>
public class CardTarget : ITarget
{
    public Card card;

    public CardTarget(Card c) => card = c;

    public Vector3? GetWorldPosition() => null;

    public Vector2Int? GetCellPosition() => null;

    public GameObject gameObject => null;
}
