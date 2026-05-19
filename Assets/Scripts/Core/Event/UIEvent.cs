/// <summary>
/// 场景切换事件，当新场景已加载完成并且遮罩动画播放完毕时触发，携带前后场景信息。
/// </summary>
public class SceneChangedEvent : GameEvent
{
    public string PreviousScene { get; private set; }
    public string CurrentScene { get; private set; }

    public SceneChangedEvent(string previousScene, string currentScene)
    {
        PreviousScene = previousScene;
        CurrentScene = currentScene;
    }
}

/// <summary>
/// 面板切换事件，当UI面板打开或关闭时触发，携带面板名称和状态信息。
/// </summary>
public class PanelSwitchedEvent : GameEvent
{
    public string[] previousPanelNames { get; private set; }
    public string[] currentPanelNames { get; private set; }

    public PanelSwitchedEvent(string[] previousPanelNames, string[] currentPanelNames)
    {
        this.previousPanelNames = previousPanelNames;
        this.currentPanelNames = currentPanelNames;
    }
}

/// <summary>
/// 玩家主动结束出牌阶段事件
/// </summary>
public class EndPlayerTurnEvent : GameEvent
{
    
}