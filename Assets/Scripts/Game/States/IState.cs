using UnityEngine;

/// <summary>
/// 状态接口，定义了游戏中各种状态需要实现的基本方法
/// </summary>
public interface IState
{
    void Enter();
    void Update();
    void Exit();
}