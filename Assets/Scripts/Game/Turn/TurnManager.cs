using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合内阶段枚举
/// </summary>
public enum TurnPhase
{
    Start,
    Draw,
    PlayerPlay,
    PlayerAction,
    Enemy,
    End
}

/// <summary>
/// 回合管理器，负责控制回合流程、阶段切换等
/// 采用状态模式实现不同阶段的逻辑分离，方便扩展和维护
/// </summary>
public class TurnManager : MonoBehaviour
{
    public int currentTurn { get; private set; } = 0;
    public int maxPlayerActions = 3;
    public int playerActionsRemaining;

    private ITurnState oldState;
    private ITurnState currentState;

    private Dictionary<TurnPhase, ITurnState> phaseStates = new Dictionary<TurnPhase, ITurnState>();

    private void Awake()
    {
        //处理订阅事件
    }

    private void Start()
    {
        phaseStates.Add(TurnPhase.Start, new StartState(this));
        phaseStates.Add(TurnPhase.Draw, new DrawState(this));
        phaseStates.Add(TurnPhase.PlayerPlay, new PlayerPlayState(this));
        phaseStates.Add(TurnPhase.PlayerAction, new PlayerActionState(this));
        phaseStates.Add(TurnPhase.Enemy, new EnemyState(this));
        phaseStates.Add(TurnPhase.End, new EndState(this));

        currentState = phaseStates[TurnPhase.End];
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public void LoadTurnData(LevelTurnData levelTurnData)
    {
        // todo
    }

/// <summary>
/// 开始新回合，前提是当前回合已经结束（End阶段），否则会有警告提示
/// 游戏开始后、回合开始前默认为End阶段
/// </summary>
    public void StartTurn()
    {
        
        if (currentState.phaseName == TurnPhase.End)
        {
            currentTurn++;
            ChangePhase(TurnPhase.Start);
            Debug.Log("第" + currentTurn + "回合开始");
        }
        else
        {
            Debug.LogWarning("当前回合未结束，无法开始新回合");
        }
    }

/// <summary>
/// 更改阶段通用方法，负责调用当前阶段的退出逻辑和新阶段的进入逻辑，并派发阶段变化事件
/// </summary>
/// <param name="newPhase"></param>
    private void ChangePhase(TurnPhase newPhase)
    {
        if (currentState != null)
        {
            oldState = currentState;
            Debug.Log("来自" + oldState.phaseName + "的请求，切换到" + newPhase + "阶段");
            currentState.Exit();
        }

        currentState = phaseStates[newPhase];
        Debug.Log("切换到" + newPhase + "阶段");
        currentState.Enter();

        GameEventChannel.Dispatch(new PhaseChangedEvent
        {
            turnNumber = currentTurn,
            oldPhase = oldState?.phaseName ?? TurnPhase.End,
            newPhase = newPhase
        });
    }
}

/// <summary>
/// 开始阶段状态类，负责处理开始阶段的逻辑
/// </summary>
class StartState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.Start;
    public StartState(TurnManager manager)
    {
        turnManager = manager;
    }

    public void Enter()
    {
        currentTurn = turnManager.currentTurn;
        Debug.Log("Entering Start Phase");
        // Initialize start phase logic here
    }

    public void Update()
    {
        // Handle start phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting Start Phase");
        // Clean up start phase logic here
    }
}

/// <summary>
/// 抽牌阶段状态类，负责处理抽牌阶段的逻辑
/// </summary>
class DrawState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.Draw;
    public DrawState(TurnManager manager)
    {
        turnManager = manager;
    }

    public void Enter()
    {
        currentTurn = turnManager.currentTurn;
        Debug.Log("Entering Draw Phase");
        // Initialize draw phase logic here
    }

    public void Update()
    {
        // Handle draw phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting Draw Phase");
        // Clean up draw phase logic here
    }
}

/// <summary>
/// 玩家操作阶段状态类，负责处理玩家操作阶段的逻辑
/// </summary>
class PlayerPlayState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.PlayerPlay;
    public PlayerPlayState(TurnManager manager)
    {
        turnManager = manager;
    }

    public void Enter()
    {
        Debug.Log("Entering Player Play Phase");
        // Initialize player play phase logic here
    }

    public void Update()
    {
        // Handle player play phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting Player Play Phase");
        // Clean up player play phase logic here
    }
}

/// <summary>
/// 角色行动阶段状态类，负责处理玩家操作阶段的逻辑
/// </summary>
class PlayerActionState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.PlayerAction;
    public PlayerActionState(TurnManager manager)
    {
        turnManager = manager;
        currentTurn = turnManager.currentTurn;
    }

    public void Enter()
    {
        Debug.Log("Entering Player Action Phase");
        // Initialize player action phase logic here
    }

    public void Update()
    {
        // Handle player action phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting Player Action Phase");
        // Clean up player action phase logic here
    }
}

/// <summary>
/// 敌人阶段状态类，负责处理敌人阶段的逻辑
/// </summary>
class EnemyState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.Enemy;
    public EnemyState(TurnManager manager)
    {
        turnManager = manager;
        currentTurn = turnManager.currentTurn;
    }

    public void Enter()
    {
        Debug.Log("Entering Enemy Phase");
        // Initialize enemy phase logic here
    }

    public void Update()
    {
        // Handle enemy phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting Enemy Phase");
        // Clean up enemy phase logic here
    }
}

/// <summary>
/// 结束阶段状态类，负责处理结束阶段的逻辑
/// 结束阶段主要负责结算、状态重置等，为下一轮的开始做好准备
/// </summary>
class EndState : ITurnState
{
    public TurnManager turnManager { get; private set; }
    public int currentTurn { get; private set; }
    public TurnPhase phaseName => TurnPhase.End;
    public EndState(TurnManager manager)
    {
        turnManager = manager;
        currentTurn = turnManager.currentTurn;
    }

    public void Enter()
    {
        Debug.Log("Entering End Phase");
        // Initialize end phase logic here
    }

    public void Update()
    {
        // Handle end phase updates here
    }

    public void Exit()
    {
        Debug.Log("Exiting End Phase");
        // Clean up end phase logic here
    }
}
