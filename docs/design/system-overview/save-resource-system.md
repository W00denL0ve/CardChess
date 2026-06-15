# 存档与资源配置系统 设计文档

> 最后更新：2026-06-15 | 作者：WoodenLove

## 一、子系统概述

- **职责**：能量/金币管理、JSON 存档读写、运行状态（`RunState`）维护、难度曲线配置
- **不负责**：场景加载、资产引用解析、卡牌/单位的具体逻辑
- **依赖模块**：事件总线（`ResourceChangedEvent`）、回合管理（Draw 阶段刷新能量）、卡牌/单位数据配置

## 二、核心类/数据结构

### 2.1 资源管理

```
ResourceManager (MonoBehaviour)
  ├─ 能量管理: currentEnergy, maxEnergy
  ├─ 金币管理: currentGold
  ├─ 牌库地址列表管理
  └─ 资源变更时派发 ResourceChangedEvent

ResourceType (enum)
  └─ Energy, Gold
```

### 2.2 存档结构

```
RunState
  ├─ playerRoster           玩家阵容（单位列表）
  ├─ gold                   金币
  ├─ maxEnergy              能量上限
  ├─ cardAddresses          卡牌地址列表（Addressables key）
  ├─ globalLevelIndex       全局关卡索引
  └─ randomSeed             随机种子

SaveManager
  ├─ PlayerPrefs: 音量、分辨率等设置
  ├─ JSON: 写入 Saves/ 目录
  ├─ SaveRun(runState) → run.json
  └─ LoadRun() → RunState
```

### 2.3 难度配置

```
RunConfig (ScriptableObject)
  ├─ 难度曲线参数
  └─ 全局进度难度计算

SpawnGroup (ScriptableObject)
  ├─ 单位列表（带权重）
  └─ 搜索半径

GameStartConfig (ScriptableObject)
  ├─ 初始角色列表
  └─ 初始卡牌列表
```

## 三、关键流程

### 3.1 新游戏流程

```text
GameManager.StartNewGame()
  → 读取 GameStartConfig
  → 创建 RunState（初始角色 + 初始卡牌）
  → ResourceManager 初始化能量/金币
  → 切换到 Map 场景
```

### 3.2 存档读写

```text
SaveRun:
  RunState → JSON → File.WriteAllText(Saves/run.json)

LoadRun:
  File.ReadAllText(Saves/run.json) → JSON → RunState
```

## 四、配置表详细规范

### 4.1 RunState

| 字段 | 类型 | 含义 |
|------|------|------|
| `playerRoster` | `List<UnitSaveData>` | 玩家阵容 |
| `gold` | `int` | 金币 |
| `maxEnergy` | `int` | 能量上限 |
| `cardAddresses` | `List<string>` | 卡牌资产地址 |
| `globalLevelIndex` | `int` | 当前关卡索引 |
| `randomSeed` | `int` | 随机种子 |

### 4.2 GameStartConfig

| 字段 | 类型 | 含义 |
|------|------|------|
| `initialCardIds` | `List<string>` | 初始卡牌 ID 列表 |
| `initialUnits` | `List<UnitConfig>` | 初始角色配置 |

### 4.3 SpawnGroup

| 字段 | 类型 | 含义 |
|------|------|------|
| `entries` | `List<WeightedEntry>` | 单位权重列表 |
| `searchRadius` | `int` | 生成搜索半径 |

## 五、错误处理与边界条件

- **存档不存在**：`LoadRun` 返回 null，`GameManager` 创建新 RunState
- **能量不足**：`ResourceManager` 检查后拒绝消耗，派发失败事件
- **牌库地址为空**：`DeckManager` 初始化为空牌库

## 六、性能注意事项

- **存档 IO**：JSON 读写只在保存/加载时发生，无运行时性能影响
- **RunState**：常驻内存，大小可控（典型 < 100KB）

## 七、测试要点 & 已知坑

- **手动测试**：创建新游戏 → 推进几回合 → 保存 → 重新加载验证状态一致
- **边界测试**：存档损坏时的容错处理、能量为 0 时出牌
- **⚠️ SaveManager**：混合 PlayerPrefs（基础层）+ JSON 存档（逻辑层），职责不单一，后续可拆分
