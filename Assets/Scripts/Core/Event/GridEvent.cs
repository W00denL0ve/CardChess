/// <summary>
/// 格子更新事件
/// </summary>
public class CellUpdatedEvent : GameEvent
{
    public int col;
    public int row;
    public int layer;
    public Cell cell;

    public CellUpdatedEvent(int col, int row)
    {
        this.col = col;
        this.row = row;
    }

    public CellUpdatedEvent(Cell cell)
    {
        this.cell = cell;
        this.col = cell.col;
        this.row = cell.row;
    }
}