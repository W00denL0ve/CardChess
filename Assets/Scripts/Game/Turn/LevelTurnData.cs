using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合行动数据资产 - 存储关卡中每回合自动执行的行动列表
/// </summary>
[CreateAssetMenu(fileName = "LevelTurnData", menuName = "Game/LevelTurnData")]
public class LevelTurnData : ScriptableObject
{
    /// <summary>所有回合的行动列表</summary>
    public List<RoundActions> rounds;

    [System.Serializable]
    public class RoundActions
    {
        public int roundNumber;

        /// <summary>使用 [SerializeReference] 支持抽象类多态序列化</summary>
        [SerializeReference]
        public List<TurnAction> actions;
    }

    /// <summary>获取指定回合的所有行动</summary>
    public List<TurnAction> GetActions(int round)
    {
        var entry = rounds?.Find(r => r.roundNumber == round);
        return entry?.actions ?? new List<TurnAction>();
    }
}