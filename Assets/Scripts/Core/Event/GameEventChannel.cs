using System.Collections.Generic;
/// <summary>
/// 所有事件的基类（可包含公共字段，如时间戳）
/// </summary>
public abstract class GameEvent { }

/// <summary>
/// 全局事件总线（静态类，也可改为单例 MonoBehaviour）
/// </summary>
public static class GameEventChannel
{
    /// <summary>
    /// 存储每个事件类型对应的监听器列表
    /// </summary>
    private static readonly Dictionary<System.Type, List<System.Delegate>> 
        listeners = new Dictionary<System.Type, List<System.Delegate>>();

    /// <summary>
    /// 注册监听器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    public static void Register<T>(System.Action<T> listener) where T : GameEvent
    {
        var type = typeof(T);
        if (!listeners.ContainsKey(type))
            listeners[type] = new List<System.Delegate>();
        listeners[type].Add(listener);
    }

    /// <summary>
    /// 注销监听器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="listener"></param>
    public static void Unregister<T>(System.Action<T> listener) where T : GameEvent
    {
        var type = typeof(T);
        if (listeners.ContainsKey(type))
            listeners[type].Remove(listener);
    }

    /// <summary>
    /// 派发事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="evt"></param>
    public static void Dispatch<T>(T evt) where T : GameEvent
    {
        var type = typeof(T);
        if (listeners.TryGetValue(type, out var delegates))
        {
            // 遍历副本，防止回调中修改列表
            foreach (var del in delegates.ToArray())
            {
                (del as System.Action<T>)?.Invoke(evt);
            }
        }
    }
}