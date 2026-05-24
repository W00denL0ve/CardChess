using System.Collections;
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
    public int drawCount = 3;

    private ITurnState oldState;
    private ITurnState currentState;

    /// <summary>
    /// 每回合恢复能量偏移量
    /// </summary>
    public int energyOffset { get; private set; } = 0;

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

    private void OnEnable()
    {
        GameEventChannel.Register<LevelEnteredEvent>(OnLevelEntered);
    }

    private void OnDisable()
    {
        GameEventChannel.Unregister<LevelEnteredEvent>(OnLevelEntered);
    }

    private void OnLevelEntered(LevelEnteredEvent evt)
    {
        Logger.Log($"[TurnManager] 关卡进入，开始第一回合");
        StartCoroutine(StartTurnRoutine());
    }

    private IEnumerator StartTurnRoutine()
    {
        yield return new WaitForSeconds(1.5f); // 等待短暂时间，确保UI动画播放完毕
        StartTurn();
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
    public void ChangePhase(TurnPhase newPhase)
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
        // Logger.Log($"Entering Start Phase (Round {turn})");
        GameEventChannel.Dispatch(new TurnStartedEvent(turn));

        // 执行当前回合的预设行动
        var actions = TurnManager.Instance.CurrentRoundActions;
        if (actions.Count > 0)
        {
            Logger.Log($"第 {turn} 回合有 {actions.Count} 个预设行动待执行");
            TurnActionExecutor.ExecuteAll(actions);
        }

        // 转入抽牌阶段
        TurnManager.Instance.ChangePhase(TurnPhase.Draw);
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
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.RefreshEnergy(TurnManager.Instance.energyOffset);
        TurnManager.Instance.StartCoroutine(DrawCardsRoutine());
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Draw Phase");
    }

    private IEnumerator DrawCardsRoutine()
    {
        int drawCount = TurnManager.Instance.drawCount;

        // 数据层先行 + 单次群组动画，完成后切阶段
        yield return DeckManager.Instance?.DrawCardsAsync(drawCount, () =>
        {
            TurnManager.Instance.ChangePhase(TurnPhase.PlayerPlay);
        });
    }
}

/// <summary>
/// 玩家出牌阶段 — 注册 CardClickedEvent，玩家点击卡牌时打出
/// </summary>
class PlayerPlayState : ITurnState
{
    public TurnPhase phaseName => TurnPhase.PlayerPlay;

    public void Enter()
    {
        Logger.Log("Entering Player Play Phase");
        GameEventChannel.Register<CardClickedEvent>(OnCardClicked);
        GameEventChannel.Register<EndPlayerTurnEvent>(OnEndTurn);
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Player Play Phase");
        GameEventChannel.Unregister<CardClickedEvent>(OnCardClicked);
        GameEventChannel.Unregister<EndPlayerTurnEvent>(OnEndTurn);
    }

    private void OnCardClicked(CardClickedEvent evt)
    {
        CardData card = evt.Card;
        if (card == null) return;

        // 检查能量是否足够
        if (!ResourceManager.Instance.SpendEnergy(card.Cost))
        {
            Logger.Log($"[PlayerPlay] 能量不足，无法打出 {card.cardName}（需要 {card.Cost}，当前 {ResourceManager.Instance.Energy}）");
            return;
        }

        Logger.Log($"[PlayerPlay] 打出卡牌: {card.cardName}（消耗 {card.Cost} 能量）");

        // 数据层：手牌 → pending
        DeckManager.Instance?.MarkCardPlayed(card);

        // 切到行动阶段（执行期间不可再次出牌）
        TurnManager.Instance.ChangePhase(TurnPhase.PlayerAction);

        // 等待卡牌飞到 pending 区后再执行效果链
        TurnManager.Instance.StartCoroutine(PlayCardWithPendingDelay(card));
    }

    private IEnumerator PlayCardWithPendingDelay(CardData card)
    {
        bool arrived = false;
        HandUI.Instance?.WaitForPendingArrival(card, () => arrived = true);
        Logger.Log($"[PlayCardWithPendingDelay] 等待卡牌到达 pending...");
        yield return new WaitUntil(() => arrived);
        Logger.Log($"[PlayCardWithPendingDelay] 卡牌已到达 pending，开始执行效果链");

        // 执行效果链，完成后回到出牌阶段
        AsyncEffectExecutor.Instance?.ExecuteCardChainsAsync(card, () =>
        {
            Logger.Log($"[PlayCardWithPendingDelay] 效果链完成回调触发");
            TurnManager.Instance.ChangePhase(TurnPhase.PlayerPlay);
        });
    }

    private void OnEndTurn(EndPlayerTurnEvent evt)
    {
        Logger.Log("[PlayerPlay] 玩家结束出牌阶段");
        TurnManager.Instance.ChangePhase(TurnPhase.Enemy);
    }
}

/// <summary>
/// 角色行动阶段 — 卡牌效果执行期间，等待执行完毕自动跳转
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
        TurnManager.Instance.StartCoroutine(ExecuteEnemyTurn());
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting Enemy Phase");
    }

    private IEnumerator ExecuteEnemyTurn()
    {
        Logger.Log("开始弃牌");

        // 弃掉不保留的手牌
        yield return DeckManager.Instance?.DiscardNonRetainedAsync();

        var enemies = LevelManager.Instance?.GetUnitsByFaction(Faction.Enemy);
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                Logger.Log("TurnManager：执行ai");
                yield return AIController.Instance.ExecuteTurn(enemy);
                yield return new WaitForSeconds(
                    AIController.Instance != null ? AIController.Instance.delayBetweenUnits : 0.5f);
            }
        }

        AIController.Instance.TickCooldowns();
        TurnManager.Instance.ChangePhase(TurnPhase.End);
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

        // 结束阶段完成后自动开始下一回合
        TurnManager.Instance.StartTurn();
    }

    public void Update() { }

    public void Exit()
    {
        Logger.Log("Exiting End Phase");
    }
}
