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
    public static TurnManager Instance { get; private set; }
    public int currentTurn { get; private set; } = 0;
    public int maxPlayerActions = 3;
    public int playerActionsRemaining;

    private ITurnState oldState;
    private ITurnState currentState;

    private Dictionary<TurnPhase, ITurnState> phaseStates = new Dictionary<TurnPhase, ITurnState>();

    /// <summary>当前关卡的回合行动数据</summary>
    private LevelTurnData turnData;

    /// <summary>获取当前回合的预设行动列表（可能为空）</summary>
    public List<TurnAction> CurrentRoundActions => turnData?.GetActions(currentTurn) ?? new List<TurnAction>();

    /// <summary>当前关卡是否有回合行动数据</summary>
    public bool HasTurnData => turnData != null;

    private void Awake()
    {
         if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //处理订阅事件
    }

    private void Start()
    {
        phaseStates.Add(TurnPhase.Start, new StartState());
        phaseStates.Add(TurnPhase.Draw, new DrawState());
        phaseStates.Add(TurnPhase.PlayerPlay, new PlayerPlayState());
        phaseStates.Add(TurnPhase.PlayerAction, new PlayerActionState());
        phaseStates.Add(TurnPhase.Enemy, new EnemyState());
        phaseStates.Add(TurnPhase.End, new EndState());

        currentState = phaseStates[TurnPhase.End];
    }

    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    /// <summary>
    /// 加载关卡回合行动数据，由 LevelManager 在关卡初始化时调用
    /// </summary>
    public void LoadTurnData(LevelTurnData levelTurnData)
    {
        turnData = levelTurnData;
        Logger.Log($"[TurnManager] 已加载回合行动数据");
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
            Logger.Log("第" + currentTurn + "回合开始");
        }
        else
        {
            Logger.LogWarning("当前回合未结束，无法开始新回合");
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
            Logger.Log("来自" + oldState.phaseName + "的请求，切换到" + newPhase + "阶段");
            currentState.Exit();
        }

        currentState = phaseStates[newPhase];
        Logger.Log("切换到" + newPhase + "阶段");
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
    public TurnPhase phaseName => TurnPhase.Start;

    public void Enter()
    {
        int turn = TurnManager.Instance.currentTurn;
        Logger.Log($"Entering Start Phase (Round {turn})");

        // 执行当前回合的预设行动
        var actions = TurnManager.Instance.CurrentRoundActions;
        if (actions.Count > 0)
        {
            Logger.Log($"第 {turn} 回合有 {actions.Count} 个预设行动待执行");
            TurnActionExecutor.ExecuteAll(actions);
        }
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Start Phase");
    }
}

/// <summary>
/// 抽牌阶段状态类，负责处理抽牌阶段的逻辑
/// </summary>
class DrawState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.Draw;

    public void Enter()
    {
        Logger.Log("Entering Draw Phase");
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Draw Phase");
    }
}

/// <summary>
/// 玩家操作阶段状态类，负责处理玩家操作阶段的逻辑
/// </summary>
class PlayerPlayState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.PlayerPlay;

    public void Enter()
    {
        Logger.Log("Entering Player Play Phase");
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Player Play Phase");
    }
}

/// <summary>
/// 角色行动阶段状态类，负责处理玩家操作阶段的逻辑
/// </summary>
class PlayerActionState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.PlayerAction;

    public void Enter()
    {
        Logger.Log("Entering Player Action Phase");
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Player Action Phase");
    }
}

/// <summary>
/// 敌人阶段状态类，负责处理敌人阶段的逻辑
/// </summary>
class EnemyState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.Enemy;

    public void Enter()
    {
        Logger.Log("Entering Enemy Phase");
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Enemy Phase");
    }
}

/// <summary>
/// 结束阶段状态类，负责处理结束阶段的逻辑
/// 结束阶段主要负责结算、状态重置等，为下一轮的开始做好准备
/// </summary>
class EndState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.End;

    public void Enter()
    {
        Logger.Log("Entering End Phase");
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting End Phase");
    }
}
