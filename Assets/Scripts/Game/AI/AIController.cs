// AI 控制器
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public float delayAfterAIAction = 0.3f;    // 每个行动（链）后的延迟

    // 运行时状态：AIDeck → 条目索引 → 剩余冷却
    private Dictionary<AIDeck, Dictionary<int, int>> cooldowns = new();
    // 运行时状态：AIDeck → 条目索引 → 已使用次数
    private Dictionary<AIDeck, Dictionary<int, int>> useCounts = new();

    // ═══ 策略权重表 ═══
    // chainCategory → (distance, selfHp, targetHp, energyEff, cooldown, scarcity)
    private static readonly float[,,] WEIGHTS = new float[,,]
    {
        // Aggressive
        { { 1.0f, 0.2f, 0.8f, 0.5f, 0.2f, 0.3f }, // Attack
          { 0.3f, 0.5f, 1.0f, 0.4f, 0.3f, 0.5f }, // Heal
          { 0.5f, 0.6f, 0.3f, 0.3f, 0.4f, 0.5f }, // Buff
          { 0.8f, 0.3f, 0.6f, 0.5f, 0.3f, 0.4f }, // Debuff
          { 0.5f, 0.5f, 0.5f, 0.3f, 0.2f, 0.3f } }, // Utility
        // Balanced
        { { 0.8f, 0.5f, 0.6f, 0.7f, 0.5f, 0.5f },
          { 0.5f, 0.7f, 0.8f, 0.6f, 0.5f, 0.6f },
          { 0.6f, 0.7f, 0.4f, 0.5f, 0.5f, 0.6f },
          { 0.7f, 0.5f, 0.5f, 0.6f, 0.5f, 0.5f },
          { 0.5f, 0.5f, 0.5f, 0.5f, 0.4f, 0.5f } },
        // Defensive
        { { 0.4f, 1.0f, 0.3f, 0.6f, 0.8f, 0.7f },
          { 0.6f, 0.9f, 0.6f, 0.7f, 0.6f, 0.8f },
          { 0.7f, 0.8f, 0.3f, 0.6f, 0.6f, 0.8f },
          { 0.5f, 0.7f, 0.4f, 0.6f, 0.7f, 0.6f },
          { 0.4f, 0.6f, 0.4f, 0.5f, 0.5f, 0.6f } }
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
    public IEnumerator ExecuteTurn(Unit enemy)
    {
        AIDeck deck = enemy.Config?.aiDeck;
        Logger.Log($"[AI] {enemy.UnitId} 开始回合");
        if (deck == null || deck.entries.Count == 0) { Logger.Log($"[AI] {enemy.UnitId} 无 AIDeck"); yield break; }

        int energy = deck.energyPerTurn;
        int safetyBreak = 20;

        while (energy > 0 && safetyBreak > 0)
        {
            safetyBreak--;

            Log($"[AI] 开始评分 (energy={energy}, safety={safetyBreak})");
            var best = SelectBestAction(deck, enemy);
            if (best.chain == null || best.target == null)
            {
                Log("评分无可用链，结束回合");
                yield break;
            }

            Log($"评分结果: 条目#{best.entryIndex} 目标={best.target.UnitId} 格子=({best.targetCell.x},{best.targetCell.y})");

            // 检查选中链是否真的可执行（冷却/能量等硬条件）
            bool canExec = best.chain.energyCost <= energy;
            if (canExec && cooldowns[deck].TryGetValue(best.entryIndex, out int remainCd) && remainCd > 0)
                canExec = false;

            if (!canExec)
            {
                Log("最佳链不可执行(能量不足/冷却中)，结束回合");
                yield break;
            }

            if (best.targetCell == enemy.GridPosition)
            {
                Log($"已到位，执行链");
                var ctx = new EffectContext
                {
                    executor = new UnitTarget(enemy),
                    executed = new UnitTarget(enemy),
                    aiSelector = (candidates) => PickTarget(candidates, enemy)
                };

                yield return AsyncEffectExecutor.Instance.ExecuteChainAI(best.chain.chain.steps, ctx);

                if (!ctx.chainBroken)
                {
                    energy -= best.chain.energyCost;
                    MarkUsed(deck, best.entryIndex);
                    Log($"执行成功，剩余能量={energy}");
                }
                else
                {
                    Log($"链断裂");
                }
            }
            else
            {
                Log($"向目标格 ({best.targetCell.x},{best.targetCell.y}) 移动");
                var fullPath = GridManager.Instance?.FindPath(enemy.GridPosition, best.targetCell);
                if (fullPath != null && fullPath.Count > 1)
                {
                    // 取前 MovePointLimit 步（含起点），截断路径
                    int maxSteps = enemy.MovePointLimit + 1;
                    var truncated = fullPath.Take(maxSteps).ToList();
                    Vector2Int dest = truncated.Last();

                    yield return enemy.MoveTo(dest, truncated);
                    // 移动完成 → 回到待机，与 MoveEffect 保持一致
                    var appearance = enemy.GetComponent<UnitAppearance>();
                    if (appearance != null) appearance.SetIdle();
                    Log($"移动到 ({dest.x},{dest.y})");
                }
                // 移动后重新评分（下一轮循环）
            }

            yield return new WaitForSeconds(delayAfterAIAction);
        }
    }

    // ====================================================================
    //  决策：枚举 (链, 目标) 组合
    // ====================================================================

    private void Log(string msg) => Logger.Log($"[AI] {msg}");

    private class ScoredAction
    {
        public AIChainEntry chain;
        public int entryIndex;
        public Unit target;
        public Vector2Int targetCell;
        public float score;
    }

    private ScoredAction SelectBestAction(AIDeck deck, Unit self)
    {
        EnsureRuntimeState(deck);

        ScoredAction best = new ScoredAction();
        float bestScore = float.MinValue;

        for (int i = 0; i < deck.entries.Count; i++)
        {
            var entry = deck.entries[i];
            if (entry?.chain == null) { Log($"条目#{i} chain为空，跳过"); continue; }

            if (entry.maxUsePerBattle > 0 &&
                useCounts[deck].TryGetValue(i, out int used) && used >= entry.maxUsePerBattle)
            { Log($"条目#{i} 次数已用完({used}/{entry.maxUsePerBattle})，跳过"); continue; }

            var candidates = GetCandidateTargets(self, entry.targetType);
            Log($"条目#{i} 候选目标数: {candidates.Count} (类型={entry.targetType})");

            foreach (var target in candidates)
            {
                if (target == null || !target.IsAlive) continue;

                Vector2Int cell = GetBestCellForTarget(entry, self, target);

                float score = ScoreAction(entry, self, target, cell, i, deck);
                Log($"  目标{target.UnitId} 格子({cell.x},{cell.y}) 评分={score:F2}");
                if (score > bestScore)
                {
                    bestScore = score;
                    best.chain = entry;
                    best.entryIndex = i;
                    best.target = target;
                    best.targetCell = cell;
                    best.score = score;
                }
            }
        }

        if (best.chain == null) Log("SelectBestAction: 未找到任何可用链");
        else Log($"SelectBestAction: 选中条目#{best.entryIndex} 目标={best.target?.UnitId} 格=({best.targetCell.x},{best.targetCell.y}) 分={best.score:F2}");
        return best;
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
                if (ss.selector is UnitSelectorAnyBySource s2) { range = s2.maxRange; return true; }
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

            var reachable = GridManager.Instance?.GetReachableCells(self.GridPosition, self.MovePointLimit);
            if (reachable != null)
            {
                Vector2Int best = self.GridPosition;
                float bestDist = float.MaxValue;
                foreach (var cell in reachable)
                {
                    float d = Mathf.Abs(cell.x - target.GridPosition.x) + Mathf.Abs(cell.y - target.GridPosition.y);
                    if (d <= range)
                    {
                        float distToSelf = Vector2Int.Distance(cell, self.GridPosition);
                        if (distToSelf < bestDist) { bestDist = distToSelf; best = cell; }
                    }
                }
                return best;
            }
        }
        return self.GridPosition;
    }

    /// <summary>根据目标类型获取候选目标列表</summary>
    private List<Unit> GetCandidateTargets(Unit self, AITargetType targetType)
    {
        var all = LevelManager.Instance?.AllUnits;
        if (all == null) { Log("GetCandidateTargets: LevelManager 或无单位"); return new List<Unit>(); }

        var result = targetType switch
        {
            AITargetType.Enemy => all.Where(u => u.Faction != self.Faction && u.IsAlive).ToList(),
            AITargetType.Ally => all.Where(u => u.Faction == self.Faction && u != self && u.IsAlive).ToList(),
            AITargetType.Self => new List<Unit> { self },
            AITargetType.Any => all.Where(u => u.IsAlive).ToList(),
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
        float selfHpNorm   = (float)self.CurrentHealth / Mathf.Max(self.MaxHealth, 1);
        float targetHpNorm = (float)target.CurrentHealth / Mathf.Max(target.MaxHealth, 1);
        // 能量消耗率：消耗/maxEnergy
        float energyNorm   = (float)entry.energyCost / Mathf.Max(deck.energyPerTurn, 1);
        // 冷却惩罚：值越大说明还需等待越久，负向减分
        float cooldownPenalty = -(cooldowns[deck].TryGetValue(entryIndex, out int cd) ? cd : 0);
        // 次数稀缺：1/次数
        float scarcityNorm = entry.maxUsePerBattle > 0 ? 1f / entry.maxUsePerBattle : 0.5f;

        // 加权求和
        float score = 0;
        score += entry.baseScore                                             * 1.0f;
        score += distNorm           * WEIGHTS[s, c, 0] * 10f;
        score += selfHpNorm         * WEIGHTS[s, c, 1] * 10f;
        score += targetHpNorm       * WEIGHTS[s, c, 2] * 10f;
        score += energyNorm         * WEIGHTS[s, c, 3] * 5f;
        score += cooldownPenalty    * WEIGHTS[s, c, 4] * 2f;
        score += scarcityNorm       * WEIGHTS[s, c, 5] * 5f;

        return score;
    }

    /// <summary>标记条目已使用</summary>
    private void MarkUsed(AIDeck deck, int index)
    {
        var entries = deck.entries;
        if (entries == null || index < 0 || index >= entries.Count) return;

        int cd = entries[index].cooldown;

        // 设置冷却
        if (cd > 0)
            cooldowns[deck][index] = cd;

        // 累计次数
        useCounts[deck].TryGetValue(index, out int count);
        useCounts[deck][index] = count + 1;
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

    private void EnsureRuntimeState(AIDeck deck)
    {
        if (!cooldowns.ContainsKey(deck))
        {
            cooldowns[deck] = new Dictionary<int, int>();
            useCounts[deck] = new Dictionary<int, int>();
        }
    }
}
