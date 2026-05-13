using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    private string levelName;

    public LevelManager(string lvlname)
    {
        this.levelName = lvlname;
    }
    public TurnManager turnManager = TurnManager.Instance; // 可由预制体绑定，或动态创建

    // private List<Unit> units;
    // private CardDeck deck;
    // private VictoryCondition victoryCondition;

    void Start()
    {
        // turnManager.OnPhaseChanged += HandlePhaseChange;
        // turnManager.StartTurn();
    }

    /// <summary>
    /// 加载关卡的具体实现，由GameManager调用
    /// </summary>
    public void Initialize(LevelData levelData)
    {
        // 调用GridManager(后续可能有EnemyManager)进行数据处理
        GridManager.Instance.LoadLevelData(levelData);
    }

    /// <summary>
    /// 处理玩家打出手牌的事件
    /// </summary>
    /// <param name="card"></param>
    /// <param name="targetPos"></param>
    public void OnPlayerUseCard(Card card, Vector3 targetPos)
    {
        // 解析目标格子
        GridManager.Instance.WorldToGrid(targetPos, out int col, out int row);
        // 应用效果...
        // 触发格子状态修改
    }
}