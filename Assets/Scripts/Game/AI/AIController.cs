// AI 控制器
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// AI 策略枚举
/// </summary>
public enum AIStrategy { Aggressive, Balanced, Defensive }

/// <summary>
/// AI 控制器 — 负责敌人单位的决策和执行
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("AI 参数")]
    public float delayBetweenUnits = 0.5f;     // 每个敌人行动间的延迟
    public float delayAfterAIAction = 0.05f;    // 每个行动（链）后的延迟

    // 每个敌人运行时获取其deck
    private Dictionary<Unit, AIDeck> decks = new();
    // 运行时状态：Unit → 条目索引 → 剩余冷却（每人独立，不因共享 AIDeck 而混淆）
    private Dictionary<Unit, Dictionary<int, int>> cooldowns = new();
    // 运行时状态：Unit → 条目索引 → 已使用次数
    private Dictionary<Unit, Dictionary<int, int>> useCounts = new();
    // 运行时状态：Unit → 剩余能量（由 ExecuteChain 内部维护）
    private Dictionary<Unit, int> remainingEnergy = new();

    // ═══ 策略权重表 ═══
    // chainCategory → (distance, selfHp, targetHp, energyEff, cooldown)
    private static readonly float[,,] WEIGHTS = new float[,,]
    {
        // Aggressive
        { { 0.2f, 0.2f, 0.8f, 0.5f, 0.2f }, // Attack
          { 0.3f, 0.5f, 1.0f, 0.4f, 0.3f }, // Heal
          { 0.5f, 0.6f, 0.3f, 0.3f, 0.4f }, // Buff
          { 0.8f, 0.3f, 0.6f, 0.5f, 0.3f }}, // Debuff
        // Balanced
        { { 0.6f, 0.5f, 0.6f, 0.7f, 0.5f },
          { 0.5f, 0.7f, 0.8f, 0.6f, 0.5f },
          { 0.6f, 0.7f, 0.4f, 0.5f, 0.5f },
          { 0.7f, 0.5f, 0.5f, 0.6f, 0.5f }},
        // Defensive
        { { 1.0f, 1.0f, 0.3f, 0.6f, 0.8f },
          { 0.6f, 0.9f, 0.6f, 0.7f, 0.6f },
          { 0.7f, 0.8f, 0.3f, 0.6f, 0.6f },
          { 0.5f, 0.7f, 0.4f, 0.6f, 0.7f }}
    };

    private static readonly Dictionary<AIStrategy, float> escapeWeight = new Dictionary<AIStrategy, float>()
    {
        {AIStrategy.Aggressive, 0.2f}, {AIStrategy.Balanced, 0.5f}, {AIStrategy.Defensive, 1f}
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ====================================================================
    //  主入口
    // ====================================================================

    /// <summary>为一个单位执行 AI 回合（协程）</summary>
    public IEnumerator ExecuteTurn(Unit unit)
    {
        decks[unit] = unit.Config.aiDeck;
        remainingEnergy[unit] = decks[unit].energyPerTurn;

        Logger.Log($"[AI] {unit.UnitId} 开始回合");

        if (decks[unit] == null || decks[unit].entries.Count == 0) { Logger.LogWarning($"[AI] {unit.UnitId} 无 AIDeck"); yield break; }

        int safetyBreak = 20;

        while (safetyBreak > 0)
        {
            safetyBreak--;

            Log($"开始评分 (energy={remainingEnergy[unit]}, safety={safetyBreak})");
            var best = SelectBestAction(unit);

            if (best.targetCell == unit.GridPosition) // 到达最佳位置
            {
                if (best.entryIndex != -2) // 不是逃跑
                {
                    // 尝试执行最佳链
                    bool result = false;
                    yield return ExecuteChain(unit, best.entryIndex, (success) => {
                        result = success;
                    });
                    if (!result)
                    {
                        // 尝试执行任意链
                        yield return TryExecuteAnyChain(unit, (success) => {
                            result = success;
                        });
                        if (!result) // 任意链都无法执行，退出循环
                            break;
                    }
                }
                else  
                {
                    Logger.LogWarning("评分出错，逃跑到原地");
                    break;
                } 
            }
            else // 没到最佳位置
            {
                // 检查移动冷却（索引 -1 表示移动行为，冷却 1 回合）
                if (cooldowns[unit].TryGetValue(-1, out int moveCd) && moveCd > 0)
                {
                    Log("移动冷却中，尝试执行任意链");
                    bool result = false;
                    yield return TryExecuteAnyChain(unit, (success) => {
                        result = success;
                    });
                    if (!result) // 任意链都无法执行，退出循环
                        break;
                }

                // 移动还没有进入冷却
                Log($"向目标格 ({best.targetCell.x},{best.targetCell.y}) 移动");
                var fullPath = GridManager.Instance?.FindPath(unit.GridPosition, best.targetCell);
                if (fullPath != null && fullPath.Count > 1)
                {
                    int maxSteps = unit.baseValue.movePointLimit + 1;
                    var truncated = fullPath.Take(maxSteps).ToList();
                    Vector2Int dest = truncated.Last();

                    yield return unit.MoveTo(dest, truncated);
                    var appearance = unit.GetComponent<UnitAppearance>();
                    if (appearance != null) appearance.SetIdle();
                    Log($"移动到 ({dest.x},{dest.y})");

                    // 设置移动冷却（冷却 1 回合，本回合内不再移动）
                    cooldowns[unit][-1] = 1;
                }
                // 移动后重新评分（下一轮循环）
            }

            Log($"评分结果: 条目#{best.entryIndex} 格子=({best.targetCell.x},{best.targetCell.y})");

            yield return new WaitForSeconds(delayAfterAIAction);
        }
    }

    // ====================================================================
    //  决策：枚举 (链, 目标) 组合
    // ====================================================================

    private void Log(string msg) => Logger.Log($"[AI] {msg}");

    private class ScoredAction
    {
        public int entryIndex;
        public Vector2Int targetCell;
        public float score;
    }

    
    /// <summary>为一个单位选择得分最高的目标位置</summary>
    private ScoredAction SelectBestAction(Unit unit)
    {
        EnsureRuntimeState(unit);

        ScoredAction best = new ScoredAction();

        // 链评分
        best = ScoreChains(decks[unit], unit);

        // 逃跑评分 — 与所有效果链并列比较
        var escape = ScoreEscape(unit);

        if (escape.score > best.score)
        {
            best.entryIndex = -2;
            best.targetCell = escape.targetCell;
            best.score = escape.score;
        }

        else Log($"SelectBestAction: 选中条目#{best.entryIndex} 格=({best.targetCell.x},{best.targetCell.y}) 分={best.score:F2}");
        return best;
    }

    /// <summary>
    /// 对某单位的每个链进行评分，返回最佳位置。
    /// </summary>
    private ScoredAction ScoreChains(AIDeck deck, Unit unit)
    {
        ScoredAction best = new ScoredAction();
        float bestScore = float.MinValue;

        for (int i = 0; i < deck.entries.Count; i++)
        {
            var entry = deck.entries[i];
            if (entry?.chain == null) { Log($"条目#{i} chain为空，跳过"); continue; }

            if (entry.maxUsePerBattle > 0 &&
                useCounts[unit].TryGetValue(i, out int used) && used >= entry.maxUsePerBattle)
            { Log($"条目#{i} 次数已用完({used}/{entry.maxUsePerBattle})，跳过"); continue; }

            // 根据目标类型获取目标
            var candidates = GetCandidateTargets(unit, entry.targetType);
            Log($"条目#{i} 候选目标数: {candidates.Count} (类型={entry.targetType})");

            foreach (var target in candidates)
            {
                if (target == null || !target.IsAlive) continue;

                Vector2Int cell = GetBestCellForTarget(entry, unit, target);

                float score = ScoreAction(entry, unit, target, cell, i, deck);
                Log($"  目标{target.UnitId} 格子({cell.x},{cell.y}) 评分={score:F2}");
                if (score > bestScore)
                {
                    bestScore = score;
                    best.entryIndex = i;
                    best.targetCell = cell;
                    best.score = score;
                }
            }
        }
        return best;
    }

    /// <summary>
    /// 对敌方每个可达格子做逃生评分，返回最佳格子
    /// escapeScore = -avgInvDistToPlayer + avgInvDistToAlly + escapeBaseScore
    /// 得分最高的格子 = 远离敌人 + 靠近友军 + 自身残血，如果在脚下得分为0
    /// </summary>
    private ScoredAction ScoreEscape(Unit unit)
    {
        var result = new ScoredAction { entryIndex = -1, score = float.MinValue };
        float bestScore = float.MinValue;
        Vector2Int bestCell = new Vector2Int();

        float selfHpNorm = (float)unit.baseValue.currentHealth / Mathf.Max(unit.baseValue.maxHealth, 1);
        float escapeBaseScore = escapeWeight[decks[unit].strategy] * (selfHpNorm > 0.01f ? 1f / selfHpNorm : 100f); // HP越低基础分越高

        // 获取所有 Hostile 和 Ally 单位
        List<Unit> hostiles = LevelManager.Instance?.GetEnemiesOf(unit, false);
        List<Unit> allies = LevelManager.Instance?.GetAlliesOf(unit, false);
        int hostileCount = hostiles.Count;
        int allyCount = allies.Count;

        // 获取所有可达格子
        var reachable = GridManager.Instance?.GetReachableCells(unit.GridPosition, unit.baseValue.movePointLimit);
        if (reachable == null || reachable.Count == 0) return result;

        foreach (var cell in reachable)
        {
            float score = ScoreEscapeCell(cell, hostiles, allies, escapeBaseScore, hostileCount, allyCount);
            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        // 如果当前位置就是最佳撤退点，得分置零(最小值)
        float localScore = ScoreEscapeCell(unit.GridPosition, hostiles, allies, escapeBaseScore, hostileCount, allyCount);
        if(localScore >= bestScore)
        {
            bestScore = float.MinValue;
        }

        result.targetCell = bestCell;
        result.score = bestScore;
        Log($"逃跑评分: 格({bestCell.x},{bestCell.y}) 分={bestScore:F2} (base={escapeBaseScore:F2})");
        return result;
    }

    /// <summary>计算某个格子的逃生评分</summary>
    private float ScoreEscapeCell(Vector2Int cell, List<Unit> hostiles, List<Unit> allies,
        float escapeBaseScore, int hostileCount, int allyCount)
    {
        float invDistSum = 0f;

        // -avgInvDistToPlayer：离敌人越近扣分越多
        if (hostileCount > 0)
        {
            float sum = 0f;
            foreach (var p in hostiles)
                sum += 1f / Mathf.Max(Vector2Int.Distance(cell, p.GridPosition), 0.5f);
            invDistSum -= sum / hostileCount;
        }

        // +avgInvDistToAlly：离友军越近加分越多
        if (allyCount > 0)
        {
            float sum = 0f;
            foreach (var a in allies)
                sum += 1f / Mathf.Max(Vector2Int.Distance(cell, a.GridPosition), 0.5f);
            invDistSum += sum / allyCount;
        }

        return invDistSum + escapeBaseScore;
    }


    /// <summary>解析链的选择器范围（返回 false 表示无距离限制）</summary>
    private bool GetChainRange(AIChainEntry entry, out int range)
    {
        range = 0;
        if (entry?.chain?.steps == null) return false;
        foreach (var step in entry.chain.steps)
        {
            if (step is SelectorStep ss && ss.selector != null)
            {
                if (ss.selector is UnitSelectorBySource s1) { range = s1.maxRange; return true; }
                // if (ss.selector is UnitSelectorAnyBySource s2) { range = s2.maxRange; return true; }
                if (ss.selector is CellAreaSelector s3) { range = s3.maxRadius; return true; }
                return false;
            }
        }
        return false;
    }

    /// <summary>获取对指定目标执行链的最佳格子</summary>
    private Vector2Int GetBestCellForTarget(AIChainEntry entry, Unit self, Unit target)
    {
        if (GetChainRange(entry, out int range) && range > 0)
        {
            float currentDist = Vector2Int.Distance(self.GridPosition, target.GridPosition);
            if (currentDist <= range) return self.GridPosition;

            var reachable = GridManager.Instance?.GetReachableCells(self.GridPosition, self.baseValue.movePointLimit);
            if (reachable != null)
            {
                Vector2Int best = self.GridPosition;
                float bestDistToTarget = float.MaxValue;
                float inRangeBestDist = float.MaxValue;

                foreach (var cell in reachable)
                {
                    float d = Mathf.Abs(cell.x - target.GridPosition.x) + Mathf.Abs(cell.y - target.GridPosition.y);
                    if (d <= range)
                    {
                        // 能进攻击范围 → 选移动步数最少的
                        float distToSelf = Vector2Int.Distance(cell, self.GridPosition);
                        if (distToSelf < inRangeBestDist)
                        { inRangeBestDist = distToSelf; bestDistToTarget = d; best = cell; }
                    }
                    else if (inRangeBestDist == float.MaxValue && d < bestDistToTarget)
                    {
                        // 没有能进范围的 → 选最靠近目标的（尽量靠近）
                        bestDistToTarget = d;
                        best = cell;
                    }
                }
                return best;
            }
        }
        return self.GridPosition;
    }

    /// <summary>根据目标类型获取候选目标列表</summary>
    private IReadOnlyList<Unit> GetCandidateTargets(Unit unit, AITargetType targetType)
    {
        var result = targetType switch
        {
            AITargetType.Hostile => LevelManager.Instance.GetEnemiesOf(unit, false),
            AITargetType.Hostile_Neutral => LevelManager.Instance.GetEnemiesOf(unit, true),
            AITargetType.Ally => LevelManager.Instance.GetAlliesOf(unit, false),
            AITargetType.Self => new List<Unit> { unit },
            AITargetType.Ally_Self => LevelManager.Instance.GetAlliesOf(unit, true),
            AITargetType.Any => LevelManager.Instance.AllUnits,
            _ => new List<Unit>()
        };
        Log($"GetCandidateTargets({targetType}): 返回 {result.Count} 个目标");
        return result;
    }

    private float ScoreAction(AIChainEntry entry, Unit self, Unit target, Vector2Int cell, int entryIndex, AIDeck deck)
    {
        int s = (int)deck.strategy;
        int c = (int)entry.category;

        // 原始值 × 归一化
        float dist = Mathf.Max(Vector2Int.Distance(cell, target.GridPosition), 0.5f);
        float distNorm     = 1f / dist;
        float selfHpNorm   = (float)self.baseValue.currentHealth / Mathf.Max(self.baseValue.maxHealth, 1);
        float targetHpNorm = (float)target.baseValue.currentHealth / Mathf.Max(target.baseValue.maxHealth, 1);
        // 能量消耗率：消耗/maxEnergy
        float energyNorm   = (float)entry.energyCost / Mathf.Max(deck.energyPerTurn, 1);
        // 冷却惩罚：值越大说明还需等待越久，负向减分
        float cooldownPenalty = -(cooldowns[self].TryGetValue(entryIndex, out int cd) ? cd : 0);

        // 加权求和
        float score = 0;
        score += entry.baseScore                       * 1f;
        score += distNorm           * WEIGHTS[s, c, 0] * 1f;
        score += selfHpNorm         * WEIGHTS[s, c, 1] * 1f;
        score += targetHpNorm       * WEIGHTS[s, c, 2] * 1f;
        score += energyNorm         * WEIGHTS[s, c, 3] * 5f;
        score += cooldownPenalty    * WEIGHTS[s, c, 4] * 2f;

        return score;
    }

    /// <summary>
    /// 检查链是否可用 → 执行 → 扣减能量/冷却/次数
    /// </summary>
    private IEnumerator ExecuteChain(Unit unit, int index, Action<bool> OnComplete)
    {
        bool success = false;
        int curEnergy = remainingEnergy[unit];
        if (decks[unit].entries[index].energyCost > curEnergy) 
        { 
            Log("能量不足，无法执行");
            OnComplete?.Invoke(success);
            yield break;
        }
        if (cooldowns[unit].TryGetValue(index, out int cd) && cd > 0)
        {
            Log("冷却中，无法执行");
            OnComplete?.Invoke(success);
            yield break;
        }

        Log($"执行链");
        var ctx = new EffectContext
        {
            executor = new UnitTarget(unit),
            executed = new UnitTarget(unit),
            aiSelector = (candidates) => PickTarget(candidates, unit)
        };

        yield return AsyncEffectExecutor.Instance.ExecuteChainAI(decks[unit].entries[index].chain.steps, ctx);

        if (!ctx.chainBroken)
        {
            MarkUsed(unit, index);
            success = true;
            Log($"执行成功");
        }
        else
        {
            Log($"链断裂");
        }
        OnComplete?.Invoke(success);
    }

    /// <summary>
    /// 遍历所有链，执行第一个可用的
    /// </summary>
    private IEnumerator TryExecuteAnyChain(Unit unit, Action<bool> OnComplete)
    {
        bool success = false;
        int curEnergy = remainingEnergy[unit];
        for (int i = 0; i < decks[unit].entries.Count; i++)
        {
            var entry = decks[unit].entries[i];
            if (entry?.chain == null) continue;
            if (entry.energyCost > curEnergy) continue;
            if (entry.maxUsePerBattle > 0 &&
                useCounts[unit].TryGetValue(i, out int used) && used >= entry.maxUsePerBattle) continue;
            if (cooldowns[unit].TryGetValue(i, out int cd) && cd > 0) continue;

            bool result = false;
            yield return ExecuteChain(unit, i, (success)=>{
                result = success;
                Log($"尝试执行链{i}，执行结果{success}");
            });
            success |= result;
        }
        OnComplete?.Invoke(success);
    }

    /// <summary>标记条目已使用</summary>
    private void MarkUsed(Unit unit, int index)
    {
        var entries = unit.Config?.aiDeck?.entries;
        if (entries == null || index < 0 || index >= entries.Count) return;

        int cd = entries[index].cooldown;

        // 设置冷却
        if (cd > 0)
            cooldowns[unit][index] = cd;
        
        // 消耗能量
        remainingEnergy[unit] -= entries[index].energyCost;

        // 累计次数
        useCounts[unit].TryGetValue(index, out int count);
        useCounts[unit][index] = count + 1;
    }

    /// <summary>所有 AIDeck 冷却减 1（回合结束时调用）</summary>
    public void TickCooldowns()
    {
        Logger.Log("[AI] TickCooldowns");
        foreach (var kvp in cooldowns)
        {
            var keys = new List<int>(kvp.Value.Keys);
            foreach (var key in keys)
            {
                if (kvp.Value[key] > 0)
                    kvp.Value[key]--;
            }
        }
    }

    // ====================================================================
    //  目标选择（给 EffectContext.aiSelector 用）
    // ====================================================================

    /// <summary>从候选中选一个目标（支持 UnitTarget 和 CellTarget）</summary>
    public ITarget PickTarget(List<ITarget> candidates, Unit self)
    {
        Logger.Log($"[AI] PickTarget: {candidates.Count} 个候选");
        // CellTarget → 选最佳格子
        if (candidates.Count > 0 && candidates[0] is CellTarget)
        {
            return PickBestCell(candidates, self, null);
        }

        // UnitTarget → 选最近目标
        Unit nearest = null;
        float minDist = float.MaxValue;
        foreach (var t in candidates)
        {
            Unit u = (t as UnitTarget)?.unit;
            if (u == null || !u.IsAlive) continue;
            float dist = Vector2Int.Distance(self.GridPosition, u.GridPosition);
            if (dist < minDist) { minDist = dist; nearest = u; }
        }
        return nearest != null ? new UnitTarget(nearest) : candidates.FirstOrDefault();
    }

    /// <summary>从候选格子中选最佳移动位置</summary>
    private ITarget PickBestCell(List<ITarget> candidates, Unit self, Vector2Int? targetPos = null)
    {
        if (candidates.Count == 0) return null;
        return candidates[0];
    }

    // ====================================================================
    //  工具
    // ====================================================================

    private void EnsureRuntimeState(Unit unit)
    {
        if (!cooldowns.ContainsKey(unit))
        {
            cooldowns[unit] = new Dictionary<int, int>();
            useCounts[unit] = new Dictionary<int, int>();
        }
    }
}
