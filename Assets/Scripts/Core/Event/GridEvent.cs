/// <summary>
/// 格子更新事件
/// </summary>
public class CellUpdatedEvent : GameEvent
{
    public int col;
    public int row;
    public int layer;

    public CellUpdatedEvent(int col, int row)
    {
        this.col = col;
        this.row = row;
    }
}