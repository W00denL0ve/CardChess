using UnityEngine;

/// <summary>
/// 放在 Animator 所在子物体上，将 AnimationEvent 转发到父级 UnitAppearance
/// </summary>
public class AnimationEventForwarder : MonoBehaviour
{
    public void OnAnimationFrame()
    {
        GetComponentInParent<UnitAppearance>()?.OnAnimationFrame();
    }
}
