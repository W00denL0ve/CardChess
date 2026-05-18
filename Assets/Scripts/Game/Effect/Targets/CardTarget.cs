using UnityEngine;

/// <summary>
/// 卡牌目标 - 包装 CardData 资产
/// </summary>
public class CardTarget : ITarget
{
    public CardData cardData;

    public CardTarget(CardData data) => cardData = data;

    public Vector3? GetWorldPosition() => null;
    public Vector2Int? GetCellPosition() => null;
    public GameObject gameObject => null;
}
