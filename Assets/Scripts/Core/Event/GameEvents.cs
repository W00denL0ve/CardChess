/// <summary>
/// 游戏开始事件
/// </summary>
public class GameStartEvent : GameEvent
{

}

/// <summary>
/// 游戏暂停事件
/// </summary>
public class GamePauseEvent : GameEvent
{

}

/// <summary>
/// 游戏继续事件
/// </summary>
public class GameResumeEvent : GameEvent
{

}

/// <summary>
/// 关卡结束事件（胜利或失败）
/// </summary>
public class LevelOverEvent : GameEvent
{
    public bool isVictory;
    public LevelOverEvent(bool victory) => isVictory = victory;
}

/// <summary>
/// 进入地图事件
/// </summary>
public class MapEnteredEvent : GameEvent
{
    
}

/// <summary>
/// 进入关卡事件
/// </summary>
public class LevelEnteredEvent : GameEvent
{
    public string levelName { get; private set; }

    public LevelEnteredEvent(string levelName)
    {
        this.levelName = levelName;
    }
}