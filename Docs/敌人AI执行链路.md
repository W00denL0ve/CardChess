## 敌人 AI 完整执行链路

### 一、总入口

```
TurnManager.ExecuteEnemyTurn()
  └─ foreach enemy in GetUnitsOf(Faction.Enemy)
      └─ yield return AIController.Instance.ExecuteTurn(enemy)
      └─ yield return WaitForSeconds(delayBetweenUnits)
  └─ AIController.Instance.TickCooldowns()
  └─ TurnManager.ChangePhase(TurnPhase.End)
```

---

### 二、`AIController.ExecuteTurn(Unit enemy)` — 单敌人回合

```
初始化: remainingEnergy[enemy] = deck.energyPerTurn
        safetyBreak = 20

while (remainingEnergy[enemy] > 0 && safetyBreak > 0):
  safetyBreak--

  ① SelectBestAction(deck, enemy)
     ├─ 遍历 deck.entries → 对每个 (条目, 目标) 评分
     │   ├─ 次数用完 → 跳过
     │   ├─ GetCandidateTargets → 候选目标列表
     │   ├─ GetBestCellForTarget → 理想格子
     │   └─ ScoreAction → 加权得分
     ├─ 逃跑评分 (独立公式)
     │   └─ ScoreEscape → 对每个可达格子算逃跑分
     └─ 返回最高分的 ScoredAction

  ② 根据 result 分支:

  ┌─ targetCell == 自身位置?
  │   ├─ entryIndex != -2? (选中了链)
  │   │   └─ ExecuteChain(enemy, chain, index)
  │   │       ├─ 检查能量 → 不足则 yield break
  │   │       ├─ 检查冷却 → 冷却中则 yield break
  │   │       └─ 执行链 (yield ExecuteChainAI)
  │   │           ├─ 成功 → remainingEnergy -= cost, MarkUsed(cd+count)
  │   │           └─ 断裂 → 什么都不扣
  │   │
  │   └─ entryIndex == -2? (逃跑胜出)
  │       └─ TryExecuteAnyChain(enemy, deck)
  │           ├─ 遍历条目找第一个可用的
  │           └─ 找到 → ExecuteChain → yield break
  │              未找到 → 空过
  │
  └─ targetCell != 自身位置? (需要移动)
      ├─ 移动冷却中? → break
      └─ FindPath → 截断到 MovePointLimit → MoveTo
          └─ 设移动冷却 cooldowns[-1] = 1

  ③ yield return WaitForSeconds(delayAfterAIAction)
```

---

### 三、关键子流程

| 方法 | 职责 | 返回 |
|---|---|---|
| `SelectBestAction` | 评分所有链 + 逃跑 | `ScoredAction` (chain/target/targetCell/score) |
| `ScoreAction` | 单条链的加权公式 (WEIGHTS + 6个因子) | `float` 分值 |
| `ScoreEscape` | 对所有可达格子算逃跑分 | `ScoredAction`(entryIndex=-2) |
| `ScoreEscapeCell` | 单个格子的逃跑分公式 | `float` 分值 |
| `GetBestCellForTarget` | 确定对某个目标的最佳站位 | `Vector2Int` |
| `ExecuteChain` | 检查→执行 | IEnumerator (yield) |
| `TryExecuteAnyChain` | 遍历找可用链兜底执行 | IEnumerator (yield) |
| `GetCandidateTargets` | 按 targetType 过滤单位 | `List<Unit>` |
| `MarkUsed` | 扣除能量 + 设置冷却 + 累计次数 | `void` |

### 四、三种执行路径总结

| 路径 | 触发条件 | 执行内容 |
|---|---|---|
| **正常执行** | `chain!=null, entryIndex>=0, targetCell==self` | `ExecuteChain` → 扣能量/冷却/次数 |
| **逃跑兜底** | `entryIndex==-2, targetCell==self` | `TryExecuteAnyChain` → 找可用链执行 |
| **移动** | `targetCell!=self` | 寻路→走路→设移动冷却，下轮循环重新评分 |

### 五、运行时状态

| 字典 | 粒度 | 用途 |
|---|---|---|
| `cooldowns[Unit][int]` | 每条链 | 链冷却回合数（-1=移动, -2=预留逃跑） |
| `useCounts[Unit][int]` | 每条链 | 链已使用次数 |
| `remainingEnergy[Unit]` | 每个敌人 | 本回合剩余能量，由 `ExecuteChain` 内部扣减 |