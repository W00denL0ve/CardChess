# 🎮 项目已完成功能清单

> **项目类型**：卡牌 + 战棋策略游戏（回合制）  
> **技术栈**：Unity (C#) + Addressables + Tilemap + ScriptableObject + DOTween + New Input System  
> **架构风格**：数据与表现分离、事件驱动、状态模式

---

## 项目文件结构

```
Assets/
├── Scripts/
│   ├── Core/                              # 核心基础设施
│   │   ├── Boot/                          #   游戏启动引导
│   │   │   ├── Bootstrapper.cs            │     启动引导器
│   │   │   └── Initializer.cs             │     初始化器
│   │   ├── Event/                         #   事件系统
│   │   │   ├── GameEventChannel.cs        │     全局事件总线
│   │   │   ├── GameEvents.cs              │     游戏生命周期事件 (LevelEntered, LevelOver 等)
│   │   │   ├── TurnEvents.cs              │     回合阶段事件
│   │   │   ├── UnitEvent.cs               │     单位事件 (死亡/移动/HP变化)
│   │   │   ├── InputEvent.cs              │     输入事件 (单击/双击/右键/ESC)
│   │   │   └── UIEvent.cs                 │     场景/面板切换事件
│   │   └── Input/                         #   输入系统
│   │       └── InputManager.cs            │     New Input System 输入管理器
│   │
│   ├── Game/                              # 游戏业务逻辑
│   │   ├── GameManager.cs                 │     游戏主管理器
│   │   ├── SaveManager.cs                 │     存档/设置管理器
│   │   │
│   │   ├── AI/                            #   AI 系统
│   │   │   ├── AIController.cs            │     AI 控制器 (评分/执行/逃跑)
│   │   │   ├── AIChainEntry.cs            │     AI 链条目数据
│   │   │   └── AIDeck.cs                  │     AI 牌组配置资产
│   │   │
│   │   ├── Card/                          #   卡牌系统
│   │   │   ├── CardData.cs                │     卡牌数据资产
│   │   │   ├── CardVisualizer.cs          │     卡牌 UI 表现
│   │   │   └── DeckManager.cs             │     牌库管理器
│   │   │
│   │   ├── Unit/                          #   单位系统
│   │   │   ├── Unit.cs                    │     战斗实体核心
│   │   │   ├── UnitAppearance.cs          │     外观表现 (动画/排序/死亡)
│   │   │   ├── UnitConfig.cs              │     配置资产
│   │   │   ├── UnitFactory.cs             │     单位工厂
│   │   │   ├── Occupation.cs              │     职业枚举
│   │   │   ├── DamageType.cs              │     伤害类型枚举
│   │   │   ├── AnimationEventForwarder.cs │     动画事件转发
│   │   │   └── AttributeInitData.cs       │     属性初始化数据
│   │   │
│   │   ├── Attribute/                     #   属性系统
│   │   │   ├── AttributeManager.cs        │     属性管理器
│   │   │   ├── Attribute.cs               │     属性值
│   │   │   └── Modifier.cs                │     修饰器 (Add/Multiply/FinalAdd/FinalMultiply)
│   │   │
│   │   ├── Buff/                          #   Buff 系统
│   │   │   ├── Buff.cs                    │     Buff 抽象基类
│   │   │   ├── BuffInstance.cs            │     Buff 实例
│   │   │   ├── BuffContainer.cs           │     Buff 容器
│   │   │   └── Buffs/                     │     具体实现
│   │   │       ├── PoisonBuff.cs          │
│   │   │       └── DefenseStanceBuff.cs   │
│   │   │
│   │   ├── Effect/                        #   效果系统 (链式架构)
│   │   │   ├── Core/                      │     核心接口
│   │   │   │   ├── ITarget.cs             │       目标接口
│   │   │   │   └── Effect.cs              │       效果基类
│   │   │   ├── EffectContext.cs           │     效果上下文
│   │   │   ├── TargetSelector.cs          │     选择器基类
│   │   │   ├── AsyncEffectExecutor.cs     │     异步效果执行器
│   │   │   ├── EffectManager.cs           │     同步效果管理器
│   │   │   ├── ChainStep.cs               │     步骤基类 (SelectorStep/ConditionStep/EffectStep)
│   │   │   ├── Selectors/                 │     选择器
│   │   │   │   ├── UnitSelector.cs        │       全局 AND
│   │   │   │   ├── UnitSelectorAny.cs     │       全局 OR
│   │   │   │   ├── UnitSelectorBySource.cs│       相对 AND
│   │   │   │   ├── UnitSelectorAnyBySource.cs│    相对 OR
│   │   │   │   └── CellAreaSelector.cs    │       区域格子
│   │   │   ├── Effects/                   │     效果实现
│   │   │   │   ├── DamageEffect.cs        │       伤害
│   │   │   │   ├── MoveEffect.cs          │       移动
│   │   │   │   ├── SwapEffect.cs          │       交换位置
│   │   │   │   └── AddBuffEffect.cs       │       添加 Buff
│   │   │   └── Targets/                   │     目标包装
│   │   │       ├── UnitTarget.cs          │
│   │   │       ├── CellTarget.cs          │
│   │   │       └── CardTarget.cs          │
│   │   │
│   │   ├── Grid/                          #   网格系统
│   │   │   ├── Data/                      │     数据层
│   │   │   │   ├── CellData.cs            │       格子数据
│   │   │   │   ├── LevelGridData.cs       │       网格数据资产
│   │   │   │   └── TerrainType.cs         │       地形枚举
│   │   │   └── Runtime/                   │     运行时
│   │   │       ├── Cell.cs                │       运行时格子
│   │   │       ├── GridManager.cs         │       网格管理器 (寻路/放置)
│   │   │       ├── GridVisualizer.cs      │       网格可视化
│   │   │       ├── PathRenderer.cs        │       路径绘制
│   │   │       └── TerrainConfig.cs       │       地形配置
│   │   │
│   │   ├── Resource/                      #   资源系统
│   │   │   └── ResourceManager.cs         │     玩家资源管理器
│   │   │
│   │   ├── Config/                        #   配置
│   │   │   └── GameStartConfig.cs         │     开局配置
│   │   │
│   │   ├── Level/                         #   关卡系统
│   │   │   ├── LevelData.cs               │     关卡数据资产
│   │   │   ├── LevelManager.cs            │     关卡管理器
│   │   │   ├── Tiles/                     │     关卡 Tile 资产
│   │   │   │   ├── ConditionTile.cs       │       胜利条件 Tile 基类
│   │   │   │   ├── KillAllEnemiesTile.cs  │
│   │   │   │   ├── SurviveRoundsTile.cs   │
│   │   │   │   ├── ProtectUnitTile.cs     │
│   │   │   │   ├── ReachGoalTile.cs       │
│   │   │   │   └── GoalTile.cs            │       目标点 Tile
│   │   │   └── Victory/                   │     胜利条件
│   │   │       ├── VictoryCondition.cs    │       条件基类
│   │   │       ├── CompositeCondition.cs  │       AND/OR 组合
│   │   │       ├── LogicOperator.cs       │       逻辑运算符枚举
│   │   │       ├── VictoryChecker.cs      │       运行时检查器
│   │   │       └── Conditions/            │       具体条件
│   │   │           ├── KillAllEnemiesCondition.cs
│   │   │           ├── SurviveRoundsCondition.cs
│   │   │           ├── ProtectUnitCondition.cs
│   │   │           └── ReachGoalCondition.cs
│   │   │
│   │   ├── Save/                          #   存档
│   │   │   └── RunState.cs                │     运行时状态
│   │   │
│   │   ├── Turn/                          #   回合系统
│   │   │   ├── TurnManager.cs             │     回合管理器 (状态机)
│   │   │   ├── LevelTurnData.cs           │     回合数据资产
│   │   │   ├── TurnAction.cs              │     回合行动类体系
│   │   │   └── TurnActionExecutor.cs      │     行动执行器
│   │   │
│   │   ├── UI/                            #   UI 系统
│   │   │   ├── UIManager.cs               │     全局 UI 管理器
│   │   │   ├── HandUI.cs                  │     手牌 UI
│   │   │   ├── HUD_UI.cs                  │     游戏内 HUD
│   │   │   ├── MainMenuUI.cs              │     主菜单
│   │   │   ├── PauseMenuUI.cs             │     暂停菜单
│   │   │   ├── MapUITemp.cs               │     地图 UI
│   │   │   ├── LoadingScreen.cs           │     加载画面
│   │   │   ├── AnimatedPanel.cs           │     面板动画
│   │   │   ├── ButtonTween.cs             │     按钮补间
│   │   │   ├── MaskAlphaController.cs     │     遮罩 Alpha
│   │   │   └── MaskRadiusAnimator.cs      │     镂空遮罩动画
│   │   │
│   │   ├── Preview/                       #   预览系统
│   │   │   └── PreviewManager.cs          │     预览管理器
│   │   │
│   │   └── Map/                           #   大地图
│   │       └── MapGenerator.cs            │     地图生成器
│   │
│   ├── Scene/                             #   场景工具
│   │   └── SceneManager.cs                │     场景加载
│   │
│   └── CameraController.cs               #   相机控制
│
├── Settings/                              #   项目设置
│   └── Input/
│       ├── GameInput.cs                   │     New Input System 包装
│       └── GameInput.inputactions         │     输入动作资产
│
├── Editor/                                #   编辑器工具
│   ├── LevelDataMenuExtractor.cs          │     关卡数据提取器
│   ├── TerrainTile.cs                     │     地形 Tile
│   ├── AIDeckEditor.cs                    │     AI 配置编辑
│   ├── CardDataEditor.cs                  │     卡牌编辑
│   ├── ScriptableObjectIconDrawer.cs      │     SO 图标覆盖
│   └── SolidColorSpriteGeneratorWindow.cs │     精灵生成器
│
├── ScriptableObjects/                     #   运行时资产
│   └── LevelData/                         │     关卡数据
│       ├── GridData/                      │       网格数据
│       └── TurnData/                      │       回合数据
│
└── *.md                                   #   架构文档
    ├── 已完成功能清单.md
    ├── 地图制作指南.md
    ├── Unit制作指南.md
    ├── 卡牌制作指南.md
    ├── 数据与表现分离设计.md
    └── GameEvent用法详解.md
```

---

## 一、核心架构

### 事件总线
- `GameEventChannel` 泛型事件总线，所有系统通过 `GameEvent` 派生类解耦通信
- `LevelEnteredEvent` / `LevelOverEvent` / `PhaseChangedEvent` / `UnitDeathEvent` / `UnitMovedEvent` / `UnitHealthChangedEvent` / `ResourceChangedEvent` 等

### 状态模式
- `TurnManager`：Start → Draw → PlayerPlay → PlayerAction → Enemy → End 六阶段状态机
- `PreviewManager`：Idle / Selecting / Preselected 三状态

---

## 二、场景与关卡

### 启动流程
- `Bootstrapper` 从 Resources 加载全局 `Manager` 预制体 + `Main Camera`，`DontDestroyOnLoad`
- 主菜单 → 新游戏 → 地图场景 → 关卡场景
- `GameManager` 统一管理游戏生命周期：`StartNewGame()` / `LoadLevelAsync()` / `BackToMainMenu()`

### 关卡初始化
- `LevelManager.Initialize(levelData, initialCards)` — 加载网格 → 回合数据 → 部署玩家单位 → 初始化牌库 → 初始化胜利条件
- 自动派发 `LevelEnteredEvent`，`TurnManager` 响应启动第一回合

### 关卡数据（Tilemap 提取管线）
- 设计师在 Tilemap 层绘制 → `Tools → Extract LevelData From Scene` → 自动生成 `LevelData` 资产
- 支持的 Tilemap 层：`Base`(地形)、`PlayerSpawn`(出生点)、`Goal`(目标点)、`WinCondition`(胜利条件)、`RoundN`(回合行动)
- 地形 Tile：`TerrainTile`；回合行动：`SpawnUnitAction` / `CellChangeAction` / `EffectApplyAction`

---

## 三、胜利条件系统（AND / OR 组合）

### 条件类
- `VictoryCondition` 抽象基类 → `KillAllEnemiesCondition` / `SurviveRoundsCondition` / `ProtectUnitCondition` / `ReachGoalCondition`
- `CompositeCondition` 递归嵌套 AND/OR，支持任意复杂逻辑
- `VictoryChecker` MonoBehaviour，监听 `UnitDeathEvent` + `PhaseChangedEvent` 自动检查，派发 `LevelOverEvent`

### Tilemap 条件配置
- `WinCondition` Tilemap 层：同一 y = AND，不同 y = OR
- 相同 Tile 的数量 = 参数值（如 `SurviveRoundsTile` × 5 = 存活 5 回合）
- 非数值参数（如 `ProtectUnitTile.targetUnitId`）提取后手动编辑

---

## 四、网格系统

### 数据与表现分离
- `LevelGridData` 资产（宽高 + CellData[]） → `GridManager` 运行时（Cell[,]）
- `GridVisualizer`：从 `cellVisualPrefab` 逐格生成方块，高亮/还原
- `PathRenderer`：对象池管理路径精灵

### 寻路
- `FindPath(start, end)` BFS 寻路（含占用/可行走检查）
- `GetReachableCells(start, maxSteps)` BFS 可达区域
- 坐标转换：`WorldToGrid()` / `GridToWorld()` / `GetWorldPosition()`
- 事件驱动格子占用：订阅 `UnitMovedEvent` / `UnitDeathEvent`

---

## 五、单位系统

### Unit（MonoBehaviour）
- `Initialize(config, pos, faction)` — 初始化属性 + Buff 容器 + 血条
- 属性：`CurrentHealth` / `MaxHealth` / `Attack` / `Intelligence` / `PhysicalDefense` / `MagicDefense` / `MovePointLimit` / `MovePoints` / `DamageBonus` / `HpPercent`
- `TakeDamage(finalDamage)` — 伤害 → 死亡判定 → 派发事件
- `Heal(amount)` — 治疗 → 派发事件
- `MoveTo(destination, path, snap)` — 异步移动协程，播放动画 → 更新位置 → 派发 `UnitMovedEvent`
- `healthBar` Slider 支持，`UpdateHealthBar()` 在 Initialize/TakeDamage/Heal 中调用

### UnitAppearance
- 动画驱动：`triggerWalk` / `triggerIdle` / `triggerTeleport` / `triggerAttack` / `triggerHit` / `triggerDead`
- `PlayWalkAnimation(path)` — 逐格走路 + `FaceTo` 朝向
- `PlayDeathAnimation()` — 死亡动画 + DOTween 渐隐所有子物体 Image/SpriteRenderer
- `SetIdle()` / `FaceTo(targetPos)` / `RefreshSortingOrder()` — Y 轴排序（`order = 预制体原始值 - gridY × 10`）
- `AnimationEventForwarder` 转发子 Animator 的 AnimationEvent

### UnitConfig（ScriptableObject）
- `unitId` / `unitName` / `occupation` / `icon` / `unitPrefab` / `defaultFaction` / `aiDeck`
- `initialAttributes` / `innateBuffs`

---

## 六、效果系统（链式架构）

### 核心
- `EffectChain` ScriptableObject：`List<ChainStep>` 步骤链
- `ChainStep` 抽象基类 → `SelectorStep` / `ConditionStep` / `EffectStep`（`[SerializeReference]` 多态）
- `EffectContext`：`executor` / `executed` / `sourceCard` / `cachedPath` / `aiSelector` / `chainBroken`

### 选择器
- `UnitSelector`（全局 AND） / `UnitSelectorAny`（全局 OR） / `UnitSelectorBySource`（相对 AND） / `UnitSelectorAnyBySource`（相对 OR）
- 过滤：`FactionMask`（Player/Enemy/Neutral）+ `OccMask`（Warrior/Rogue/Mage）+ `maxRange` + `nameFilter`
- `CellAreaSelector`：区域格子选择，支持 Circle/Square/Cross/Ring，可选单位/可行走过滤

### 执行器
- `AsyncEffectExecutor.ExecuteCardChainsAsync(card)` — 玩家卡牌异步执行入口
- `ExecuteChainAI(steps, ctx)` — AI 专用协程入口
- `ExecuteStepsRoutine` — 逐步解析 SelectorStep → ConditionStep → EffectStep
- AI 模式 vs 玩家模式：通过 `context.aiSelector` 区分（AI 直接选最近目标）

### 效果
- `DamageEffect`：修饰器管线（base=Attack，add→multiply→finalAdd→finalMultiply），支持 `IAnimatedEffect`
- `MoveEffect`：`Unit.MoveTo` + `SetIdle`
- `SwapEffect` / `AddBuffEffect`

---

## 七、AI 系统

### 数据与配置
- `AIDeck` ScriptableObject：`energyPerTurn` / `strategy`(Aggressive/Balanced/Defensive) / `entries`
- `AIChainEntry`：`chain` / `energyCost` / `cooldown` / `maxUsePerBattle` / `targetType` / `category` / `baseScore`

### 评分决策
- `SelectBestAction(unit)`：`ScoreChains` 遍历链条目评分 + `ScoreEscape` 逃跑评分，取最高分
- `ScoreAction` 加权公式：5 因子 × WEIGHTS 表（distance, selfHp, targetHp, energyEff, cooldown）
- `ScoreEscape` 逃跑公式：`escapeScore = -avgInvDistToPlayer + avgInvDistToAlly + 1/selfHpNorm`
- `GetBestCellForTarget`：启发式找最优站位（在攻击范围内 → 选最近；不在 → 选最靠近）

### 执行
- `ExecuteTurn(unit)`：while 循环 → 评分 → 移动/执行链/逃跑兜底
- `ExecuteChain(unit, index)`：检查能量+冷却 → `ExecuteChainAI` → `MarkUsed`（扣能量+设冷却+累计次数）
- `TryExecuteAnyChain(unit)`：遍历找可用链兜底执行
- 移动冷却：索引 -1，冷却 1 回合
- 状态字典 `cooldowns[Unit][int]` / `useCounts[Unit][int]` / `remainingEnergy[Unit]` 按单位隔离
- `TickCooldowns()` 每回合减 1

### AI 编辑器
- `AIDeckEditor`：动态条目布局 + 预设按钮（近战/远程/治疗）

---

## 八、卡牌系统

### 数据
- `CardData` ScriptableObject：`cardName` / `cost` / `destination` / `chains` / `colorPreset`

### 表现
- `CardVisualizer`：`Bind` / `RefreshUI` / DOTween 入场动画 / 能量消耗颜色 / 发光覆盖
- `HandUI`：手牌池管理、布局排列、抽牌/弃牌动画、能量显示（DOTween 数字滚动）、卡牌费用颜色刷新

### 管理
- `DeckManager`：`deck` / `hand` / `discardPile` / `pendingPlay`
- `DrawCard()` / `DrawCardsAsync(count)` / `MarkCardPlayed()` / `CompleteCard()` / `DiscardNonRetainedAsync()`
- `Initialize(initialCards)` 由 `LevelManager` 注入

---

## 九、资源系统

- `ResourceManager` 单例：`Energy`/`MaxEnergy`/`Gold`/`DeckCardIds`
- `SpendEnergy` / `GainEnergy` / `RefreshEnergy` 能量操作
- `LoadFromRunState()` / `SaveToRunState()` 存档读写
- `GameStartConfig` ScriptableObject：开局初始角色 + 卡牌配置

---

## 十、属性与 Buff 系统

### AttributeManager
- `AddAttribute` / `SetBaseValue` / `GetFinalValue` / `AddModifier` / `RemoveModifier`
- 四层修饰器叠加：Add → Multiply → FinalAdd → FinalMultiply

### Buff 系统
- `Buff` 抽象基类，回调以 `BuffInstance` 为参数
- `BuffContainer`：`ApplyBuff`（堆叠/刷新）、`RemoveBuff`、`OnTurnStart/End`（自动过期）
- 具体 Buff：`PoisonBuff` / `DefenseStanceBuff`

---

## 十一、存档系统

### RunState（JSON 持久化）
- `roster`（UnitSaveData 列表）：`assetAddress` / `currentHp` / `maxHp` / `level` / `exp`
- `maxEnergy` / `gold` / `deckCardIds` / `globalStageIndex` / `randomSeed`

### SaveManager
- `NewRun(roster)` / `SaveRun()` / `LoadRun()` / `GetPlayerRoster()`
- `ResourceManager.SaveToRunState()` 同步资源数据
- 玩家阵容从存档加载，部署到关卡出生点（随机配对）

---

## 十二、相机系统

- `CameraController`：透视固定俯角，平移/缩放通过 `targetPoint` + `currentDistance` 控制
- 键盘/鼠标右键拖动平移，滚轮缩放，边界约束
- `FocusOnTarget(target, duration)`：禁用玩家控制 → lerp `targetPoint` 到目标 → 推近 `currentDistance`
- `OnSceneLoaded`：自动清理多余 MainCamera

---

## 十三、输入系统

- `InputManager`（New Input System）：`GameInput` 包装类，内嵌 InputActionAsset
- 单击/双击/右键/ESC → `GameEventChannel.Dispatch` 分发
- `Physics.Raycast` 检测格子和单位（cellLayerMask / unitLayerMask）

---

## 十四、UI 系统

- `UIManager`：面板注册/层级栈/背景遮罩/镂空转场遮罩（`MaskRadiusAnimator`）
- `PanelSwitchedEvent` 广播面板切换
- `HandUI` / `HUD_UI` / `MainMenuUI` / `PauseMenuUI` / `LoadingScreen` / `MapUITemp`
- 动画组件：`AnimatedPanel` / `ButtonTween` / `MaskAlphaController`

---

## 十五、编辑器工具

- `LevelDataMenuExtractor`：Tilemap → ScriptableObject 一键提取（地形/回合/出生点/目标点/胜利条件）
- `AIDeckEditor`：AI 配置自定义 Inspector
- `CardDataEditor`：卡牌颜色预设 Inspector
- `SolidColorSpriteGeneratorWindow`：纯色 + 圆角矩形辉光精灵生成器
- `ScriptableObjectIconDrawer`：SO 资产的图标覆盖

---

## 十六、Tile 资产

- `TerrainTile` / `PlayerSpawnTile` / `GoalTile` / `EnemySpawnTile` / `CellChangeTile`
- 胜利条件 Tile：`ConditionTile`(基) → `KillAllEnemiesTile` / `SurviveRoundsTile` / `ProtectUnitTile` / `ReachGoalTile`

---

## 十七、架构文档

- `地图制作指南.md`：Tilemap 层命名、地形绘制、回合行动、胜利条件配置、提取流程
- `Unit制作指南.md`：Sprite → UnitConfig → 预制体 → Animator → AI
- `卡牌制作指南.md`：CardData 创建与效果链配置
- `数据与表现分离设计.md` / `GameEvent用法详解.md`

---

*更新日期：2026-05-22*
