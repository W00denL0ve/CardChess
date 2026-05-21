using UnityEngine;

/// <summary>
/// 胜利条件检查器 — 挂载到 LevelManager 同级
/// 监听关键事件，在适当时机检查根条件
/// </summary>
public class VictoryChecker : MonoBehaviour
{
    public static VictoryChecker Instance { get; private set; }

    /// <summary>当前关卡的根条件（由 LevelManager.Initialize 传入）</summary>
    public VictoryCondition RootCondition { get; private set; }

    /// <summary>关卡结果是否已确定（防止重复触发）</summary>
    public bool IsLevelOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameEventChannel.Register<UnitDeathEvent>(OnUnitDeath);
        GameEventChannel.Register<PhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        GameEventChannel.Unregister<UnitDeathEvent>(OnUnitDeath);
        GameEventChannel.Unregister<PhaseChangedEvent>(OnPhaseChanged);
    }

    /// <summary>由 LevelManager 在关卡初始化时调用</summary>
    public void Initialize(VictoryCondition root)
    {
        RootCondition = root;
        IsLevelOver = false;
        RootCondition?.Initialize();
    }

    /// <summary>由 TurnManager 在阶段切换时调用</summary>
    public void OnPhaseEnd(TurnPhase phase)
    {
        // 玩家行动结束 或 敌方回合结束时检查
        if (phase == TurnPhase.PlayerAction || phase == TurnPhase.Enemy)
            CheckWin();
    }

    private void OnPhaseChanged(PhaseChangedEvent evt)
    {
        // 敌方回合结束时检查（此时已走过 Enemy→End 的切换）
        if (evt.newPhase == TurnPhase.End)
            CheckWin();
    }

    private void OnUnitDeath(UnitDeathEvent evt)
    {
        CheckWin();
    }

    /// <summary>检查胜利/失败条件</summary>
    public void CheckWin()
    {
        if (IsLevelOver || RootCondition == null) return;

        if (RootCondition.IsMet())
        {
            IsLevelOver = true;
            OnVictory();
        }
        else if (RootCondition.IsImpossible())
        {
            IsLevelOver = true;
            OnDefeat();
        }
    }

    private void OnVictory()
    {
        Logger.Log("[Victory] 🏆 胜利！条件已达成");
        GameEventChannel.Dispatch(new LevelOverEvent(true));
        // TODO: 转胜利结算界面
    }

    private void OnDefeat()
    {
        Logger.Log("[Victory] 💀 失败！所有胜利条件均不可达成");
        GameEventChannel.Dispatch(new LevelOverEvent(false));
        // TODO: 转失败结算界面
    }

    /// <summary>关卡结束时清理</summary>
    public void Cleanup()
    {
        RootCondition?.Cleanup();
        RootCondition = null;
        IsLevelOver = false;
    }
}
