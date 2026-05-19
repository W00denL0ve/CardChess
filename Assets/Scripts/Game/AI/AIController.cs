using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        if (deck == null || deck.entries.Count == 0) yield break;

        // 每回合支持多次行动，直到没有可选链
        int maxAttempts = 10; // 防死循环
        int attempts = 0;
        while (attempts < maxAttempts)
        {
            attempts++;

            // 找最近的玩家单位作为目标
            Unit target = FindNearestEnemyOf(enemy);
            if (target == null) yield break;

            // 选一条效果链
            AIChainEntry selected = SelectChain(deck, enemy, target);
            if (selected?.chain == null) yield break;

            // 构建 AI 上下文
            var ctx = new EffectContext
            {
                executor = new UnitTarget(enemy),
                executed = new UnitTarget(enemy),
                aiSelector = (candidates) => PickTarget(candidates, enemy)
            };

            yield return AsyncEffectExecutor.Instance.ExecuteChainAI(selected.chain.steps, ctx);

            yield return new WaitForSeconds(delayAfterAIAction);
        }
    }

    // ====================================================================
    //  决策
    // ====================================================================

    /// <summary>从 AIDeck 中选一条符合条件的链</summary>
    private AIChainEntry SelectChain(AIDeck deck, Unit self, Unit target)
    {
        EnsureRuntimeState(deck);

        var scored = deck.entries
            .Select((entry, index) => new { entry, index, score = ScoreEntry(entry, index, deck, self, target) })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ToList();

        if (scored.Count == 0) return null;

        var best = scored[0];
        MarkUsed(deck, best.index);
        return best.entry;
    }

    /// <summary>给一条目打分（≤0 表示不可用）</summary>
    private float ScoreEntry(AIChainEntry entry, int index, AIDeck deck, Unit self, Unit target)
    {
        // 冷却中？
        if (cooldowns[deck].TryGetValue(index, out int cd) && cd > 0) return -1;

        // 次数用完了？
        if (entry.maxUsePerBattle > 0 &&
            useCounts[deck].TryGetValue(index, out int used) && used >= entry.maxUsePerBattle)
            return -1;

        // 血量条件
        float hpRatio = (float)self.CurrentHealth / self.MaxHealth;
        if (entry.hpThreshold > 0f && hpRatio > entry.hpThreshold) return -1;

        // 距离条件
        float dist = GridManager.Instance != null
            ? Vector2Int.Distance(self.GridPosition, target.GridPosition)
            : int.MaxValue;
        if (dist < entry.minRange) return -1;
        if (entry.maxRange > 0 && dist > entry.maxRange) return -1;

        // 基础分 = priority，血量条件满足时额外加分
        float score = entry.priority;
        if (entry.hpThreshold > 0f && hpRatio <= entry.hpThreshold)
            score += 100f;  // 血量条件触发时优先

        return score;
    }

    /// <summary>标记条目已使用</summary>
    private void MarkUsed(AIDeck deck, int index)
    {
        var entries = deck.entries;
        if (entries == null || index < 0 || index >= entries.Count) return;

        // 设置冷却
        if (entries[index].cooldown > 0)
            cooldowns[deck][index] = entries[index].cooldown;

        // 累计次数
        useCounts[deck].TryGetValue(index, out int count);
        useCounts[deck][index] = count + 1;
    }

    /// <summary>所有 AIDeck 冷却减 1（回合结束时调用）</summary>
    public void TickCooldowns()
    {
        foreach (var kvp in cooldowns)
        {
            var keys = kvp.Value.Keys.ToList();
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

    /// <summary>从候选中选一个目标</summary>
    public ITarget PickTarget(List<ITarget> candidates, Unit self)
    {
        // 默认策略：选最近的目标
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

    private Unit FindNearestEnemyOf(Unit self)
    {
        var enemies = LevelManager.Instance?.GetEnemiesOf(self);
        if (enemies == null || enemies.Count == 0) return null;

        Unit nearest = null;
        float minDist = float.MaxValue;
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            float dist = Vector2Int.Distance(self.GridPosition, e.GridPosition);
            if (dist < minDist) { minDist = dist; nearest = e; }
        }
        return nearest;
    }
}
