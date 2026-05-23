using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    public string triggerHeal = "Heal";
    public string triggerCast = "Cast";

    private Animator animator;
    private Unit unit;

    private System.Action onHitCallback;

    void Awake()
    {
        unit = GetComponent<Unit>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // 初始排序：Y 越大（越靠上）排序值越小，画在后面
        RefreshSortingOrder();
    }

    void OnEnable()
    {
        GameEventChannel.Register<
        UnitDeathEvent>(OnDeath);
    }

    void OnDisable()
    {
        GameEventChannel.Unregister<UnitDeathEvent>(OnDeath);
    }

    /// <summary>根据网格 Y 刷新所有 SpriteRenderer 的排序顺序</summary>
    public void RefreshSortingOrder()
    {
        if (unit == null) return;
        int yOffset = -unit.GridPosition.y * 10;

        if (_baseOrders == null)
        {
            // 首次调用：缓存预制体原始 order，后续只加 Y 偏移
            _baseOrders = new Dictionary<SpriteRenderer, int>();
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                _baseOrders[sr] = sr.sortingOrder;
        }

        foreach (var kvp in _baseOrders)
        {
            if (kvp.Key != null)
                kvp.Key.sortingOrder = kvp.Value + yOffset;
        }
    }
    private Dictionary<SpriteRenderer, int> _baseOrders;

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

    /// <summary>播放受治疗动画，等待播完</summary>
    public IEnumerator PlayHeal()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerHeal);
        yield return WaitForCurrentClip(); 
    }

    /// <summary>播放施法动画，等待播完</summary>
    public IEnumerator PlayCast()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerCast);
        yield return WaitForCurrentClip();
    }

    /// <summary>播放死亡动画，同时渐隐所有子物体，完成后销毁</summary>
    public IEnumerator PlayDeathAnimation()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerDead);

        // 同时渐隐所有子物体的 Image 和 SpriteRenderer
        float fadeDuration = 1.0f;
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images) img.DOFade(0f, fadeDuration);
        var sprites = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in sprites) sr.DOFade(0f, fadeDuration);

        // 等待死亡动画播完（渐隐同时进行）
        float startTime = Time.time;
        yield return WaitForCurrentClip();
        // 确保渐隐至少播完
        float elapsed = Time.time - startTime;
        if (elapsed < fadeDuration)
            yield return new WaitForSeconds(fadeDuration - elapsed);
    }

    /// <summary>回到待机动画</summary>
    public void SetIdle()
    {
        if (animator == null) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("idle")) return; // 已经在待机动画了
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
    //  效果执行方与被执行方动画同步 — 由 AnimationEvent 调用
    // ====================================================================

    /// <summary>
    /// 效果调用，效果决定事件帧如何处理。
    /// </summary>
    public void SetAnimationFrameAction(Action action)
    {
        onHitCallback = action;
    }

    /// <summary>
    /// Animation Event 回调 — 在动画的触发帧调用
    /// 请在 Animation Clip 中合适时间点添加 AnimationEvent
    /// </summary>
    public void OnAnimationFrame()
    {
        onHitCallback?.Invoke();
        onHitCallback = null; // 清空以防二次调用
    }

    // ====================================================================
    //  事件响应
    // ====================================================================

    private void OnDeath(UnitDeathEvent evt)
    {
        if (evt.Unit != unit) return;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return PlayDeathAnimation();
        // 动画完毕，销毁 GameObject（LevelManager 已在事件中 Unregister）
        yield return new WaitForSeconds(20f);
        Destroy(gameObject);
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
