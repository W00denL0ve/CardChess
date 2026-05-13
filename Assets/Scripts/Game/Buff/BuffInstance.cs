public class BuffInstance
{
    public Buff buff;
    public int remainingDuration;

    public BuffInstance(Buff buff, int duration)
    {
        this.buff = buff;
        this.remainingDuration = duration;
    }

    public void DecrementDuration()
    {
        remainingDuration--;
    }

    public bool IsExpired()
    {
        return remainingDuration <= 0;
    }
}