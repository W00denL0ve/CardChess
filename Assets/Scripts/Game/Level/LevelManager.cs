using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    private string levelName;

    public LevelManager(string lvlname)
    {
        this.levelName = lvlname;
    }
    private TurnManager turnManager; // 初始化时搜索
    private GridManager gridManager; // 初始化时搜索

    // private List<Unit> units;
    // private CardDeck deck;
    // private VictoryCondition victoryCondition;

    void Awake()
    {
         if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 加载关卡的具体实现，由GameManager调用
    /// </summary>
    public void Initialize(LevelData levelData)
    {
        // 获取关卡内管理器
        turnManager = FindObjectOfType<TurnManager>();
        gridManager = FindObjectOfType<GridManager>();

        // 调用GridManager处理格子数据
        gridManager.LoadGridData(levelData.gridData);
        Debug.Log("棋盘加载完成");
        
        // 调用TurnManager处理回合内数据
        turnManager.LoadTurnData(levelData.turnData);
        Debug.Log("回合信息加载完成");
    }

    /// <summary>
    /// 处理玩家打出手牌的事件
    /// </summary>
    /// <param name="card"></param>
    /// <param name="targetPos"></param>
    public void OnPlayerUseCard(Card card, Vector3 targetPos)
    {
        // 解析目标格子
        gridManager.WorldToGrid(targetPos, out int col, out int row);
        // 应用效果...
        // 触发格子状态修改
    }

    /// <summary>
    /// 获取指定单位的所有敌人
    /// </summary>
    public List<Unit> GetEnemiesOf(Unit unit)
    {
        // TODO: 根据阵营系统实现
        Debug.LogWarning("GetEnemiesOf 尚未完全实现");
        return new List<Unit>();
    }

    /// <summary>
    /// 获取指定单位的所有友方
    /// </summary>
    public List<Unit> GetAlliesOf(Unit unit)
    {
        // TODO: 根据阵营系统实现
        Debug.LogWarning("GetAlliesOf 尚未完全实现");
        return new List<Unit>();
    }
}