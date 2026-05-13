using System.Collections.Generic;
using System;
using UnityEngine;

public enum TargetType
{
    Card,
    Self,
    Friendly,
    Enemy,
    GridCell
}

[System.Serializable]
public class EffectContext
{
    public TargetType sourceType; // Card, Self, Friendly, Enemy, GridCell，源类型
    public bool sourceSelectable; // 是否需要玩家选择源，true则显示范围让玩家选择，false则直接根据源类型使用相应的源
    public List<Vector2Int> sourceRangeList; // 如果sourceSelectable为true，则根据sourceType生成相应的范围列表供玩家选择
    public Character sourceCharacter;
    public Cell sourceCell;
    public CardData sourceCard;
    public TargetType targetType;
    public bool targetSelectable;
    public List<Vector2Int> targetRangeList;
    public Character targetCharacter;
    public Cell targetCell;
    public CardData targetCard;
    public List<int> intParams; // 具体效果执行时的整型参数列表，规定第一个参数为回合数（层数），第二个参数为伤害值，第三个参数为治疗值，第四个参数为护盾值，第五个参数为属性增减值
    public List<float> floatParams; // 具体效果执行时的浮点型参数列表，规定第一个参数为属性增减百分比
    public List<string> stringParams; // 其他参数
}