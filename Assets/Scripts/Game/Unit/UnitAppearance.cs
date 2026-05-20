using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位外观表现组件 — 动画、位移、视觉反馈
/// 动画统一使用 Animator Trigger 驱动
/// </summary>
public class UnitAppearance : MonoBehaviour
{
    [Header("移动动画参数")]
    public float moveSpeed = 2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animator Trigger 名称")]
    public string triggerWalk = "Walk";
    public string triggerIdle = "Idle";
    public string triggerTeleport = "Teleport";
    public string triggerAttack = "Attack";
    public string triggerHit = "Hit";
    public string triggerDead = "Dead";

    private Animator animator;
    private Unit unit;

    /// <summary>由效果在 PlayAttack 前注册，AnimationEvent 在击打帧触发</summary>
    private Unit pendingHitTarget;
    private System.Action onHitCallback;

    void Awake()
    {
        unit = GetComponent<Unit>();
        animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        GameEventChannel.Register<UnitDeathEvent>(OnDeath);
    }

    void OnDisable()
    {
        GameEventChannel.Unregister<UnitDeathEvent>(OnDeath);
    }

    // ====================================================================
    //  公开协程 — 由效果链的 PlayAnimation 调用
    // ====================================================================

    /// <summary>沿路径逐格走动（Walk 动画循环，不等待）</summary>
    public IEnumerator PlayWalkAnimation(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) yield break;

        if (animator != null) animator.SetTrigger(triggerWalk);

        float stepDuration = 1f / moveSpeed;
        for (int i = 0; i < path.Count - 1; i++)
        {
            FaceTo(path[i + 1]);
            Vector3 from = GridToWorld(path[i]);
            Vector3 to = GridToWorld(path[i + 1]);

            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = moveCurve.Evaluate(elapsed / stepDuration);
                transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }
            transform.position = to;
        }
    }

    /// <summary>瞬移</summary>
    public IEnumerator PlayTeleportAnimation(Vector3 targetPosition)
    {
        if (animator != null) animator.SetTrigger(triggerTeleport);
        transform.position = targetPosition;
        yield return null;
    }

    /// <summary>播放攻击动画，等待整个 Clip 播完</summary>
    public IEnumerator PlayAttack()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerAttack);
        yield return WaitForCurrentClip();
    }

    /// <summary>播放受击动画，等待播完</summary>
    public IEnumerator PlayHitReaction()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerHit);
        yield return WaitForCurrentClip();
    }

    /// <summary>播放死亡动画，等待播完</summary>
    public IEnumerator PlayDeathAnimation()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerDead);
        yield return WaitForCurrentClip();
    }

    /// <summary>回到待机动画</summary>
    public void SetIdle()
    {
        if (animator == null) return;
        animator.SetTrigger(triggerIdle);
    }

    /// <summary>等待指定名称的动画状态播完</summary>
    public IEnumerator WaitForAnimation(string stateName)
    {
        if (animator == null) yield break;

        // 等一帧让 Trigger 生效
        yield return null;

        // 等待进入该状态
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            yield return null;

        // 等待播完
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
    }

    /// <summary>等待当前正在播放的动画播完</summary>
    private IEnumerator WaitForCurrentClip()
    {
        if (animator == null) yield break;
        yield return null; // 等 Trigger 生效
        var state = animator.GetCurrentAnimatorStateInfo(0);
        float t = 0f;
        while (t < state.length)
        {
            t += Time.deltaTime;
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }
    }

    // ====================================================================
    //  击打帧同步 — 由 AnimationEvent 调用
    // ====================================================================

    /// <summary>攻击前注册受击目标及扣血回调，供 AnimationEvent 使用</summary>
    public void RegisterHitFrameTarget(Unit target, System.Action onHit = null)
    {
        pendingHitTarget = target;
        onHitCallback = onHit;
        if (target != null)
            FaceTo(target.GridPosition);
    }

    /// <summary>
    /// Animation Event 回调 — 在攻击动画的击打帧调用
    /// 请在 Animation Clip 中合适时间点添加 AnimationEvent
    /// </summary>
    public void OnHitFrame()
    {
        // 先扣血（同步，随动画帧立即生效）
        onHitCallback?.Invoke();
        onHitCallback = null;

        // 再播受击动画（异步协程）
        if (pendingHitTarget == null) return;
        var app = pendingHitTarget.GetComponent<UnitAppearance>();
        if (app != null)
        {
            app.FaceTo(unit.GridPosition);
            StartCoroutine(app.PlayHitReaction());
        }
        pendingHitTarget = null;
    }

    // ====================================================================
    //  事件响应
    // ====================================================================

    private void OnDeath(UnitDeathEvent evt)
    {
        if (evt.Unit != unit) return;
        StartCoroutine(PlayDeathAnimation());
    }

    // ====================================================================
    //  方向
    // ====================================================================

    /// <summary>面向目标方向（通过翻转 X 缩放）</summary>
    public void FaceTo(Vector2Int targetPos)
    {
        if (animator == null) return;
        Vector2Int diff = targetPos - unit.GridPosition;
        if (diff.x == 0) return;

        Vector3 scale = animator.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (diff.x > 0 ? 1 : -1);
        animator.transform.localScale = scale;
    }

    // ====================================================================
    //  工具
    // ====================================================================

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return UnitFactory.GetWorldPosition(gridPos, unit?.Config);
    }
}
