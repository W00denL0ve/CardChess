using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 胜利条件 — [SerializeReference] 多态基类
/// 所有条件类均为可序列化普通类（非 MonoBehaviour / ScriptableObject）
/// </summary>
[System.Serializable]
public abstract class VictoryCondition
{
    [TextArea] public string description;

    /// <summary>是否满足胜利条件</summary>
    public abstract bool IsMet();

    /// <summary>是否已不可能满足（用于判定失败）</summary>
    public abstract bool IsImpossible();

    /// <summary>关卡开始时调用，绑定事件等</summary>
    public virtual void Initialize() { }

    /// <summary>关卡结束时调用，解绑事件</summary>
    public virtual void Cleanup() { }
}
