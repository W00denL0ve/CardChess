/// <summary>
/// 玩家资源变化事件
/// </summary>
public class ResourceChangedEvent : GameEvent
{
    public ResourceType type;
    public int oldValue;
    public int newValue;
}
