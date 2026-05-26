using System;
using System.Collections;
using System.Collections.Generic;
using GLTFast.Schema;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

// 基础属性
[Serializable]
public struct UnitBaseValue
{    
    public int currentHealth;
    public int maxHealth;
    public int attack;
    public int intelligence;
    public int physicalDefense;
    public int magicDefense;
    public int movePointLimit;
    public int movePoints;
    public int hasMoved;
}

// 朝向枚举
public enum FacingDirection { Up, Down, Left, Right }

public class Unit : MonoBehaviour, ILongPressTarget
{
    // 配置
    [SerializeField] private string unitId;
    [SerializeField] private string unitName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Occupation occupation;

    // 运行时身份
    public string UnitId => unitId;
    public string UnitName => unitName;
    public Sprite Icon => icon;
    public Occupation Occupation => occupation;
    public Faction Faction { get; private set; }
    public bool IsAlive { get; private set; }

    // 修饰器系统
    public ModifierManager modifierManager = new();

    // 属性
    [SerializeField]
    public UnitBaseValue baseValue;

    // 外观组件
    public UnitAppearance Appearance { get; private set; }

    // Buff 容器
    public BuffContainer BuffContainer { get; private set; }

    /// <summary>血量百分比 0~1</summary>
    public float HpPercent => baseValue.maxHealth > 0 ? (float)baseValue.currentHealth / baseValue.maxHealth : 0f;
    
    public UnitConfig Config { get; private set; }

    // 网格位置（由 GridManager 设置）
    public Vector2Int GridPosition { get; internal set; }

    // 当前朝向
    public FacingDirection FacingDirection { get; internal set; }

    // 防御查询
    public int GetDefenseFor(DamageType type) => type == DamageType.Physical ? baseValue.physicalDefense : baseValue.magicDefense;

    private void Start()
    {
        GameEventChannel.Register<TurnStartedEvent>(OnTurnStarted);
    }

    private void OnDestroy()
    {
        GameEventChannel.Unregister<TurnStartedEvent>(OnTurnStarted);
    }

    public Vector3 GetScreenPosition() => UnityEngine.Camera.main?.WorldToScreenPoint(transform.position) ?? Vector3.zero;

    public void OnTurnStarted(TurnStartedEvent evt)
    {
        baseValue.hasMoved = 0;
    }


    // 初始化
    public void Initialize(UnitConfig config, Vector2Int gridPos, Faction? overrideFaction = null)
    {
        Config = config;
        unitId = config.unitId;
        unitName = config.unitName;
        icon = config.icon;
        occupation = config.occupation;
        Faction = overrideFaction ?? config.defaultFaction;
        GridPosition = gridPos;

        // 随机初始朝向
        FacingDirection = (FacingDirection)UnityEngine.Random.Range(0, 4);

        IsAlive = true;
        baseValue = config.initialValue;
        BuffContainer = new BuffContainer(this);
        foreach (var buff in config.innateBuffs)
            BuffContainer.ApplyBuff(buff, new UnitTarget(this));

        // 表现层初始化
        Appearance = GetComponent<UnitAppearance>();
        Appearance?.UpdateHealthBar();
        Appearance?.SyncFacingDirection(FacingDirection);
    }

    /// <summary>更新朝向, 根据新坐标 - 旧坐标</summary>
    /// <param name="diff">新坐标 - 旧坐标</param>
    public void UpdateFacingDirection(Vector2Int diff)
    {
        if (diff == Vector2Int.zero) return;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            // 水平优势 → 左/右
            FacingDirection = diff.x > 0 ? FacingDirection.Right : FacingDirection.Left;
        else if (Mathf.Abs(diff.y) > Mathf.Abs(diff.x))
            // 垂直优势 → 上/下
            FacingDirection = diff.y > 0 ? FacingDirection.Up : FacingDirection.Down;
        else
            // 绝对值相等 → 保持原有轴向
            if (FacingDirection == FacingDirection.Left || FacingDirection == FacingDirection.Right)
                FacingDirection = diff.x > 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                FacingDirection = diff.y > 0 ? FacingDirection.Up : FacingDirection.Down;

        Appearance?.SyncFacingDirection(FacingDirection);
    }

    /// <summary>
    /// 方位判定方法，根据给定的Unit，依据其朝向、自身位置、目标位置计算出方位，允许Buff修改判定
    /// </summary>
    public AttackPosition GetAttackPositionFromTarget(Unit executed)
    {
        // 先计算出攻单位相对位置
        Vector2Int diff = GridPosition - executed.GridPosition;
        int x = diff.x, y = diff.y;
        // 先计算出攻击方位
        AttackPosition attackPosition = executed.FacingDirection switch
        {
            // 被攻击者面朝上
            FacingDirection.Up => (y + Math.Abs(x) < 0) ? AttackPosition.Back : // 后攻击
                                  (y >= Math.Abs(x) ? AttackPosition.Front : AttackPosition.Side),
            // 被攻击者面朝下
            FacingDirection.Down => y > Math.Abs(x) ? AttackPosition.Back : // 后攻击
                                    (y + Math.Abs(x) <= 0 ? AttackPosition.Front : AttackPosition.Side),
            // 被攻击者面朝左
            FacingDirection.Left => x > Math.Abs(y) ? AttackPosition.Back : // 后攻击
                                    (x + Math.Abs(y) <= 0 ? AttackPosition.Front : AttackPosition.Side),
            // 被攻击者面朝右
            FacingDirection.Right => (x + Math.Abs(y) < 0) ? AttackPosition.Back : // 后攻击
                                      (x >= Math.Abs(y) ? AttackPosition.Front : AttackPosition.Side),
            _ => AttackPosition.Front
        };

        // 使用 BuffContainer 封装的前置回调，允许 Buff 修改攻击方位
        attackPosition = BuffContainer.OnBeforeAttackPosition(attackPosition);
        // 同样访问受击者的 BuffContainer 封装的前置回调
        attackPosition = executed.BuffContainer.OnBeforeHitPosition(attackPosition);

        return attackPosition;
    }

    /// <summary> 伤害（由效果系统调用，finalDamage 已扣除类型防御）</summary>
    public void TakeDamage(int finalDamage, EffectContext context = null)
    {
        if (!IsAlive || finalDamage <= 0) return;

        // 使用 BuffContainer 封装的前置回调，允许 Buff 修改伤害值
        BuffContainer.OnBeforeDamageTaken(ref finalDamage, context);

        int oldHealth = baseValue.currentHealth;
        int newHealth = Mathf.Clamp(oldHealth - finalDamage, 0, baseValue.maxHealth);
        baseValue.currentHealth = newHealth;
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, baseValue.maxHealth));

        if (newHealth <= 0)
        {
            IsAlive = false;
            LevelManager.Instance.HandleUnitDeath(this, context);
        }
        else
        {
            // 使用 BuffContainer 封装的后置回调
            BuffContainer.OnAfterDamageTaken(finalDamage, context);
        }

        // 视觉效果
        Appearance?.UpdateHealthBar();
    }

    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="context"></param>
    public void Heal(int amount, EffectContext context = null)
    {
        if (!IsAlive || amount <= 0) return;
        int oldHealth = baseValue.currentHealth;
        int newHealth = Mathf.Clamp(oldHealth + amount, 0, baseValue.maxHealth);
        baseValue.currentHealth = newHealth;
        GameEventChannel.Dispatch(new UnitHealthChangedEvent(this, oldHealth, newHealth, baseValue.maxHealth));
        Appearance?.UpdateHealthBar();
    }


    /// <summary>
    /// 移动到目标格子 — 数据驱动，每步先更新数据再驱动表现
    /// 经过格子触发 UnitStepOffEvent / UnitStepOntoEvent
    /// 到达后触发 UnitMovedEvent
    /// </summary>
    public IEnumerator MoveTo(Vector2Int destination, List<Vector2Int> path, bool snap = false)
    {
        if (!IsAlive) yield break;

        // ── 瞬移 ──
        if (snap || path == null || path.Count < 2)
        {
            Vector2Int fromPos = GridPosition;
            GridPosition = destination;
            if (Appearance != null)
                yield return Appearance.PlayTeleportAnimation(GridToWorld(destination));
            GameEventChannel.Dispatch(new UnitMovedEvent(this, fromPos, destination));
            yield break;
        }

        // ── 逐格移动 ──
        Vector2Int startPos = GridPosition;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int fromPos = path[i];
            Vector2Int toPos = path[i + 1];
            bool isLastStep = i == path.Count - 2;

            // 数据层：更新位置 + 朝向
            GridPosition = toPos;
            UpdateFacingDirection(toPos - fromPos);

            // 音效
            AudioManager.Instance.PlayLoopSound(AudioName.walkSound);

            // 表现层：单格动画
            if (Appearance != null)
                yield return Appearance.AnimateStep(fromPos, toPos);

            // 累计步数
            baseValue.hasMoved++;
        }

        // 移动完成
        Appearance?.RefreshSortingOrder();
        GameEventChannel.Dispatch(new UnitMovedEvent(this, startPos, destination));
        AudioManager.Instance.StopLoopSound();
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return UnitFactory.GetWorldPosition(gridPos, Config);
    }

    /// <summary>
    /// 获得行动力
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="ignoreLimit"></param> 
    /// <param name="context"></param>
    public void AcquireMovePoint(int amount, bool ignoreLimit = false, EffectContext context = null)
    {
        if (!IsAlive) return;
        //Logger.Log($"Unit: {UnitId}试图获得{amount}点行动力，" + (ignoreLimit ? "忽略" : "约束于") + "行动力上限");
        int limit = baseValue.movePointLimit;
        int points = Mathf.Clamp(baseValue.movePoints + amount, 0, limit);
        baseValue.movePoints = points;
        //Logger.Log($"Unit: {UnitId}获得{points}点行动力");
        GameEventChannel.Dispatch(new UnitAcquireMovePointEvent(this, points));
    }
}
