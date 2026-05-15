using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 自定义调试信息输出
/// </summary>
public static class Logger
{
    [Conditional("ENABLE_LOG")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("ENABLE_LOG")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("ENABLE_LOG")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }
}