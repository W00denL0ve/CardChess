using UnityEngine;

/// <summary>
/// 长按目标
/// </summary>
public interface ILongPressTarget
{
    /// <summary>获取目标在屏幕上的位置（用于定位 UI）</summary>
    Vector3 GetScreenPosition();
    GameObject gameObject { get; }
}