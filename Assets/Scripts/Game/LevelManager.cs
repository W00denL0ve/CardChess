using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    private string levelName;

    public LevelManager(string lvlname)
    {
        this.levelName = lvlname;
    }
    public TurnManager turnManager; // 可由预制体绑定，或动态创建
    public GridManager gridManager; // 通过单例获取

    // private List<Unit> units;
    // private CardDeck deck;
    // private VictoryCondition victoryCondition;

    void Start()
    {
        // turnManager.OnPhaseChanged += HandlePhaseChange;
        turnManager.StartTurn();
    }

    /// <summary>
    /// 加载关卡的具体实现，由GameManager调用
    /// </summary>
    public void Initialize(LevelData levelData)
    {
        
    }

    public void OnPlayerUseCard(Card card, Vector3 targetPos)
    {
        // 解析目标格子
        gridManager.WorldToGrid(targetPos, out int col, out int row);
        // 应用效果...
        // 触发格子状态修改
    }
}