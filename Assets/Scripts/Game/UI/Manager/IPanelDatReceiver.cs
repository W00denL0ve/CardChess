/// <summary>
/// 面板数据接收接口，展示时需要接受数据的面板需要实现这个接口
/// </summary>
public interface IPanelDataReceiver
{
    void OnReceiveData(object data);
}