using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// 单位外观表现组件 — 动画、位移、视觉反馈
/// 动画统一使用 Animator Trigger 驱动
/// </summary>
public class UnitAppearance : MonoBehaviour
{
    [Header("移动动画参数")]
    public float moveSpeed = 2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("血量显示")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("方向指示器")]
    [SerializeField] private Transform directionIndicator;

    private Image[] images;
    private SpriteRenderer[] sprites;
    private TextMeshProUGUI[] texts;

    // 各组件初始 Alpha（用于生成/死亡动画的目标值）
    private float[] imageAlphas;
    private float[] spriteAlphas;
    private float[] textAlphas;

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
        // 获取显示组件（sprite、image、text 等）
        images = GetComponentsInChildren<Image>(true);
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        texts = GetComponentsInChildren<TextMeshProUGUI>(true);

        // 缓存各组件初始 Alpha（生成动画以此为终点，死亡动画以此为起点）
        imageAlphas = Array.ConvertAll(images, img => img.color.a);
        spriteAlphas = Array.ConvertAll(sprites, sr => sr.color.a);
        textAlphas = Array.ConvertAll(texts, text => text.color.a);

        // 初始排序：Y 越大（越靠上）排序值越小，画在后面
        RefreshSortingOrder();

        // 播放生成动画
        PlaySpawnAnimation();
    }

    void OnEnable()
    {
        GameEventChannel.Register<UnitDeathEvent>(OnDeath);
    }

    void OnDisable()
    {
        GameEventChannel.Unregister<UnitDeathEvent>(OnDeath);
    }

    /// <summary>更新血量显示</summary>
    public void UpdateHealthBar()
    {
        int currentHp = unit.baseValue.currentHealth;
        int maxHp = unit.baseValue.maxHealth;
        // 血量百分比
        float hpPercent = (float)currentHp / Mathf.Max(maxHp, 1f);
        if (healthBar != null)
            healthBar.value = hpPercent;
        
        // 更新数字显示
        if (healthText != null)
            healthText.text = $"{currentHp}/{maxHp}";
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

    /// <summary>单格移动动画（数据层已更新，表现层跟随插值）</summary>
    /// <param name="from">起点格子</param>
    /// <param name="to">目标格子</param>
    /// <param name="speed">移动速度（格/秒）</param>
    public IEnumerator AnimateStep(Vector2Int from, Vector2Int to, float speed = 2f)
    {
        if (animator != null) animator.SetTrigger(triggerWalk);

        if (speed <= 0) speed = moveSpeed;
        float stepDuration = 1f / speed;

        RefreshSortingOrder();

        Vector3 fromWorld = GridToWorld(from);
        Vector3 toWorld = GridToWorld(to);

        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / stepDuration);
            transform.position = Vector3.Lerp(fromWorld, toWorld, t);
            yield return null;
        }
        transform.position = toWorld;
    }

    /// <summary>瞬移</summary>
    public IEnumerator PlayTeleportAnimation(Vector3 targetPosition)
    {
        if (animator != null) animator.SetTrigger(triggerTeleport);
        transform.position = targetPosition;
        yield return null;
    }

    /// <summary>播放攻击动画，等待整个 Clip 播完</summary>
    public IEnumerator PlayAttack(DamageType damageType)
    {
        if (animator == null) yield break;
        animator.SetTrigger(damageType == DamageType.Physical ? triggerAttack : triggerCast);
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

    [Header("生成/死亡动画")]
    [SerializeField] private float spawnDuration = 0.5f;
    [SerializeField] private float deathFadeDuration = 1.0f;

    /// <summary>播放生成动画，使用DOTween</summary>
    public void PlaySpawnAnimation()
    {
        // 从 alpha = 0 渐变到各自初始值
        for (int i = 0; i < images.Length; i++) { var c = images[i].color; c.a = 0f; images[i].color = c; }
        for (int i = 0; i < sprites.Length; i++) { var c = sprites[i].color; c.a = 0f; sprites[i].color = c; }
        for (int i = 0; i < texts.Length; i++) { var c = texts[i].color; c.a = 0f; texts[i].color = c; }

        for (int i = 0; i < images.Length; i++) images[i].DOFade(imageAlphas[i], spawnDuration);
        for (int i = 0; i < sprites.Length; i++) sprites[i].DOFade(spriteAlphas[i], spawnDuration);
        for (int i = 0; i < texts.Length; i++) texts[i].DOFade(textAlphas[i], spawnDuration);
    }

    /// <summary>播放死亡动画，同时渐隐所有子物体，完成后销毁</summary>
    public IEnumerator PlayDeathAnimation()
    {
        if (animator == null) yield break;
        animator.SetTrigger(triggerDead);

        // 从当前 Alpha 渐隐到 0
        foreach (var img in images) img.DOFade(0f, deathFadeDuration);
        foreach (var sr in sprites) sr.DOFade(0f, deathFadeDuration);
        foreach (var text in texts) text.DOFade(0f, deathFadeDuration);

        // 等待死亡动画播完（渐隐同时进行）
        float startTime = Time.time;
        yield return WaitForCurrentClip();
        // 确保渐隐至少播完
        float elapsed = Time.time - startTime;
        if (elapsed < deathFadeDuration)
            yield return new WaitForSeconds(deathFadeDuration - elapsed);
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
    //  方向（表现层跟随数据层）
    // ====================================================================

    /// <summary>
    /// 根据数据层 FacingDirection 同步视觉效果
    /// </summary>
    [Header("转向动画")]
    [SerializeField] private float turnDuration = 0.15f;

    public void SyncFacingDirection(FacingDirection dir)
    {
        if (animator == null) return;

        // 计算目标 X 缩放
        float targetScaleX = animator.transform.localScale.x;
        Quaternion targetIndicatorRot = Quaternion.identity;

        switch (dir)
        {
            case FacingDirection.Left:
                targetScaleX = -Mathf.Abs(targetScaleX);
                targetIndicatorRot = Quaternion.Euler(90, 0, 180);
                break;
            case FacingDirection.Right:
                targetScaleX = Mathf.Abs(targetScaleX);
                targetIndicatorRot = Quaternion.Euler(90, 0, 0);
                break;
            case FacingDirection.Up:
                targetIndicatorRot = Quaternion.Euler(90, 0, 90);
                break;
            case FacingDirection.Down:
                targetIndicatorRot = Quaternion.Euler(90, 0, -90);
                break;
        }

        // 平滑过渡
        animator.transform.DOScaleX(targetScaleX, turnDuration).SetEase(Ease.OutQuad);
        if (directionIndicator != null)
            directionIndicator.DOLocalRotateQuaternion(targetIndicatorRot, turnDuration).SetEase(Ease.OutQuad);
    }

    // ====================================================================
    //  工具
    // ====================================================================

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return UnitFactory.GetWorldPosition(gridPos, unit?.Config);
    }
}
