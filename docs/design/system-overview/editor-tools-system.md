# 编辑器工具系统 设计文档

> 最后更新：2026-06-15 | 作者：WoodenLove

## 一、子系统概述

- **职责**：Tilemap→ScriptableObject 一键提取管线、自定义 Inspector 绘制（多态步骤/颜色预设/行为链）、纯色精灵生成
- **不负责**：运行时游戏逻辑、关卡初始化
- **依赖模块**：关卡系统（生成 `LevelData` 资产）、卡牌系统（`CardData` 编辑增强）、AI 系统（`AIDeck` 编辑）

## 二、核心类/数据结构

### 2.1 工具列表

| 工具 | 类型 | 说明 |
|------|------|------|
| `LevelDataMenuExtractor` | 编辑器脚本 | 从 Tilemap 场景一键提取关卡数据 |
| `CardDataEditor` | PropertyDrawer | 卡牌颜色预设和数据显示增强 |
| `AIDeckEditor` | PropertyDrawer | AI 行为链条目编辑/预设 |
| `ChainStepDrawer` | PropertyDrawer | 效果链步骤的多态 Inspector 绘制 |
| `ScriptableObjectIconDrawer` | PropertyDrawer | SO 图标显示 |
| `SolidColorSpriteGeneratorWindow` | EditorWindow | 生成纯色/圆角辉光精灵 |

### 2.2 Tilemap 层规范

| Tilemap 层 | 提取目标 | 说明 |
|-----------|---------|------|
| `Base` | `LevelGridData` | 基础地形（TerrainTile） |
| `PlayerSpawn` | `playerSpawnPositions` | 玩家出生点标记 |
| `Goal` | `goalPositions` | 目标点标记 |
| `WinCondition` | `rootCondition` | 胜利条件标记 → 组合树 |
| `UnitSpawn` | 敌方出生点 | 敌方单位初始位置 |
| `CellChange` | 地形变化 | 回合中地形变化标记 |
| `RoundN` | `LevelTurnData` | 第N回合预设行动 |

## 三、关键流程

### 3.1 提取管线

```text
Tools → Extract LevelData From Scene
  ├─ 扫描场景中所有 Tilemap 层
  ├─ Base 层 → LevelGridData (宽高 + CellData[])
  │    └─ TerrainTile.isWalkable / moveCost → CellData
  ├─ PlayerSpawn 层 → List<Vector2Int>
  ├─ Goal 层 → List<Vector2Int>
  ├─ WinCondition 层 → VictoryCondition 组合树
  │    └─ 同 y = AND，不同 y = OR
  │    └─ 相同 Tile 数量 = 参数值
  ├─ RoundN 层 → TurnAction 列表
  │    ├─ SpawnUnitAction → UnitSpawn 层解析
  │    ├─ CellChangeAction → CellChange 层解析
  │    └─ EffectApplyAction → 指定格子应用效果
  └─ 自动注册 Addressables
```

## 四、配置表详细规范

### 4.1 TerrainTile (自定义 TileBase)

| 字段 | 类型 | 含义 |
|------|------|------|
| `isWalkable` | `bool` | 是否可行走 |
| `moveCost` | `int` | 移动消耗 |
| `terrainType` | `TerrainType` | 地形类型 |

### 4.2 WinCondition Tile 映射

| Tile 类型 | 胜利条件 | 参数 |
|----------|---------|------|
| `KillAllEnemiesTile` | `KillAllEnemiesCondition` | 无 |
| `SurviveRoundsTile` | `SurviveRoundsCondition` | Tile 数量 = 回合数 |
| `ProtectUnitTile` | `ProtectUnitCondition` | `targetUnitId`（手动编辑） |
| `ReachGoalTile` | `ReachGoalCondition` | Goal 层位置 |
| `ConditionTile` | AND/OR | 同一 y=AND，不同 y=OR |

## 五、错误处理与边界条件

- **无 Tilemap 层**：提取器跳过缺失层，已存在的层正常提取
- **WinCondition 层无标记**：使用默认胜利条件（全歼敌人）
- **RoundN 层不连续**：跳过中间缺失的回合号

## 六、性能注意事项

- **提取仅在编辑器执行**：无运行时开销
- **WinCondition 组合树**：使用 Tile 位置推导 AND/OR 关系，无需额外配置

## 七、测试要点 & 已知坑

- **手动测试**：创建测试 Tilemap 场景 → 提取 → 检查生成的 LevelData 资产
- **边界测试**：空 Tilemap、仅 Base 层（无敌人/无胜利条件）、超大棋盘
- **注意**：`EffectApplyAction` 当前直接应用单个效果，后续可改为完整效果链引用
