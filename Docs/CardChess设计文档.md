# CardChess 设计文档

## 一、开发环境

| 项目 | 内容 |
|---|---|
| 项目类型 | 卡牌 + 战棋策略游戏 |
| 游戏引擎 | Unity 2022.3.30f1c1 |
| 开发语言 | C# |
| 主要技术 | ScriptableObject、Addressables、Tilemap、New Input System、UGUI、TextMeshPro、DOTween |
| 主要场景 | `Boot`、`MainMenu`、`Map`、`Levels/LevelHome` |
| 主要资源目录 | `Assets/Scripts`、`Assets/ScriptableObjects`、`Assets/Prefabs`、`Assets/Scenes` |

## 二、项目简介

CardChess 是一款回合制卡牌战棋游戏。玩家在棋盘关卡中操控己方单位，通过打出手牌触发移动、攻击、治疗、Buff 等效果链，与敌方单位进行战斗。项目采用ECS架构，将卡牌、单位、关卡、AI 行为、地形和胜利条件配置为 ScriptableObject 资产，运行时由管理器加载并驱动表现。

游戏整体流程为：启动引导 -> 主菜单 -> 新游戏 -> 地图 -> 进入关卡 -> 回合战斗 -> 胜利/失败结算。关卡由 Tilemap 编辑器绘制，再通过编辑器工具提取为运行时使用的 `LevelData`、`LevelGridData` 和 `LevelTurnData`，从而实现编辑阶段和运行阶段的解耦。

## 三、设计目标

1. 将卡牌构筑与棋盘站位结合，使玩家既要规划资源消耗，也要判断移动路径、攻击范围和目标优先级。
2. 使用效果链系统统一表达玩家卡牌和敌人 AI 技能，减少重复逻辑。
3. 使用 ScriptableObject 管理静态配置，使卡牌、单位、AI、关卡和地形能够在 Inspector 中直接扩展。
4. 使用事件总线拆分系统依赖，让输入、UI、回合、单位、网格、胜利判断等模块通过事件协作。
5. 使用 Tilemap 作为关卡编辑工具，运行时只加载提取后的数据资产，降低场景耦合。

## 四、玩法说明

### 1. 开始游戏

玩家从主菜单点击开始游戏后，`GameManager.StartNewGame()` 会读取 `GameStartConfig` 中的初始角色和初始卡牌，创建新的 `RunState`，初始化资源，并切换到地图场景。地图生成完成后，系统派发 `MapEnteredEvent`。

### 2. 进入关卡

玩家在地图界面选择关卡后，`GameManager.LoadLevelAsync()` 异步加载关卡场景和对应的 Addressable `LevelData`。关卡加载完成后，`LevelManager.Initialize()` 依次完成棋盘加载、回合数据加载、玩家单位部署、牌库初始化和胜利条件初始化。

### 3. 回合流程

战斗采用自动推进的回合状态机：

```text
Start -> Draw -> PlayerPlay -> PlayerAction -> PlayerPlay -> Enemy -> End -> 下一回合
```

各阶段职责如下：

| 阶段 | 作用 |
|---|---|
| Start | 派发回合开始事件，执行本回合预设行动，如刷怪、改地形、应用格子效果 |
| Draw | 刷新能量并抽牌 |
| PlayerPlay | 玩家可以点击手牌出牌，也可以点击结束回合 |
| PlayerAction | 卡牌效果链执行中，等待动画和效果完成 |
| Enemy | 弃掉不保留手牌，敌方单位逐个执行 AI 行动 |
| End | 进入回合收尾并自动开启下一回合 |

### 4. 出牌与效果

玩家点击手牌后，系统先检查能量是否足够。如果能量足够，卡牌从手牌区移动到 pending 区，然后由 `AsyncEffectExecutor` 顺序执行卡牌上的多条效果链。效果链由三类步骤组成：

| 步骤 | 作用 |
|---|---|
| SelectorStep | 从场上单位或格子中选择目标 |
| ConditionStep | 判断效果链是否继续 |
| EffectStep | 执行实际效果，如伤害、移动、治疗、加 Buff、交换位置 |

一张卡牌可以有多条效果链，每条链独立创建上下文并顺序执行。选择器返回单个候选时会自动确认，返回多个候选时交给预览系统等待玩家点击。

### 5. 敌人行动

敌人由 `AIController` 统一执行。每个敌方单位的 `UnitConfig` 可以挂接一个 `AIDeck`，其中包含若干 `AIChainEntry`。AI 会根据策略类型、目标距离、自身血量、目标血量、能量消耗、冷却等因素为可用链条评分，选择最优行动或移动位置。AI 技能仍然复用效果链，因此敌人与玩家使用同一套底层效果系统。

### 6. 胜利与失败

胜利条件由 `VictoryCondition` 体系表达，可配置全歼敌人、坚守回合、保护单位、到达目标点等条件，并通过 `CompositeCondition` 组合 AND / OR 逻辑。`VictoryChecker` 监听单位死亡和阶段变化事件，在条件满足时派发 `LevelOverEvent`，由 `GameManager` 展示胜利或失败 UI。

## 五、总体流程设计

```text
Boot 场景
  -> Bootstrapper 实例化全局 Manager 和 Main Camera
  -> Initializer 补全 PlayerPrefs 默认设置
  -> 加载 MainMenu

MainMenu 场景
  -> 点击开始游戏
  -> GameManager 创建 RunState
  -> 加载 Map

Map 场景
  -> MapGenerator 生成地图
  -> 玩家选择关卡
  -> 加载关卡场景和 LevelData

关卡场景
  -> LevelManager 初始化棋盘、回合、单位、牌库、胜利条件
  -> TurnManager 开始第一回合
  -> 玩家出牌与敌人 AI 轮流行动
  -> VictoryChecker 判定胜负
```

## 六、核心架构设计

### 1. 启动与全局管理

`Bootstrapper` 负责从 `Resources/Prefabs` 中实例化全局 `Manager` 和 `Main Camera`，并通过 `DontDestroyOnLoad` 让它们跨场景保留。`Initializer` 负责初始化 PlayerPrefs 中的设置项。`GameManager` 负责新游戏、进入关卡、暂停、返回主菜单和关卡结算。

主要脚本：

| 脚本 | 作用 |
|---|---|
| `Bootstrapper.cs` | 启动引导、加载主菜单、应用用户设置 |
| `Initializer.cs` | 初始化默认设置 |
| `GameManager.cs` | 游戏主流程控制 |
| `SceneManager.cs` | 场景异步加载 |
| `SaveManager.cs` | PlayerPrefs 设置与 JSON 存档 |

### 2. 事件系统

项目通过 `GameEventChannel` 实现泛型事件总线。输入、UI、回合、单位、网格、资源、关卡等系统通过事件通信，减少直接引用。

典型事件包括：

| 事件 | 作用 |
|---|---|
| `MapEnteredEvent` | 进入地图 |
| `LevelEnteredEvent` | 进入关卡 |
| `TurnStartedEvent` | 回合开始 |
| `TurnPhaseChangedEvent` | 回合阶段变化 |
| `CardClickedEvent` / `CardPlayedEvent` / `CardDrawnEvent` | 卡牌交互与牌库变化 |
| `UnitMovedEvent` / `UnitDeathEvent` / `UnitHealthChangedEvent` | 单位位置、生死、生命值变化 |
| `ResourceChangedEvent` | 能量、金币等资源变化 |
| `LevelOverEvent` | 关卡胜负结束 |

### 3. 数据与表现分离

项目的核心配置都以 ScriptableObject 存储，运行时管理器读取数据后生成逻辑对象和表现对象。

| 数据资产 | 说明 |
|---|---|
| `CardData` | 卡牌名称、描述、消耗、去向、颜色、效果链 |
| `UnitConfig` | 单位 ID、名称、职业、阵营、预制体、初始属性、先天 Buff、AI 牌组 |
| `AIDeck` | 敌人 AI 的可选效果链、策略、能量和评分参数 |
| `LevelData` | 关卡总数据，引用棋盘数据、回合数据、出生点、目标点、胜利条件 |
| `LevelGridData` | 棋盘宽高与每格地形数据 |
| `LevelTurnData` | 每回合预设行动 |
| `RunConfig` | 难度曲线与全局进度难度计算 |
| `GameStartConfig` | 新游戏初始角色和初始卡牌 |

表现层则由 `GridVisualizer`、`UnitAppearance`、`CardVisualizer`、`HandUI`、`PreviewManager` 等脚本负责。数据变更先发生，表现再通过动画、UI 刷新和事件响应同步。

### 4. 棋盘系统

`GridManager` 负责运行时棋盘逻辑。它从 `LevelGridData` 构建二维 `Cell[,]`，提供坐标转换、格子查询、占用管理、BFS 寻路和可达区域查询。

关键能力：

| 能力 | 说明 |
|---|---|
| `LoadGridData()` | 加载关卡棋盘数据并重建可视化 |
| `WorldToGrid()` / `GridToWorld()` | 世界坐标与格子坐标转换 |
| `FindPath()` | 根据可行走性和占用情况寻路 |
| `GetReachableCells()` | 获取指定步数内的可达格子 |
| `PlaceUnit()` | 将单位放入格子 |
| `HandleUnitMoved()` / `HandleUnitDeath()` | 通过事件维护格子占用 |

### 5. 关卡系统

关卡编辑阶段使用 Tilemap；运行阶段使用提取后的 ScriptableObject 数据。设计师在场景中绘制 `Base`、`PlayerSpawn`、`Goal`、`WinCondition`、`RoundX` 等 Tilemap 层，然后通过 `Tools -> Extract LevelData From Scene` 生成数据资产。

关卡资产关系：

```text
LevelData
  ├─ LevelGridData     棋盘地形
  ├─ LevelTurnData     回合预设行动
  ├─ playerSpawnPositions
  ├─ goalPositions
  └─ rootCondition     胜利条件树
```

`LevelTurnData` 中的行动包括：

| 行动 | 说明 |
|---|---|
| `SpawnUnitAction` | 在指定格子生成单位，支持 SpawnGroup 和搜索半径 |
| `CellChangeAction` | 修改格子地形、高度或可行走属性 |
| `EffectApplyAction` | 在指定格子应用效果 |

### 6. 卡牌与效果链系统

`CardData` 是卡牌的核心数据。每张卡牌包含基础信息和 `List<EffectChain>`。`EffectChain` 内部通过 `[SerializeReference]` 保存多态步骤，支持灵活组合。

#### 6.1 多态步骤与 Inspector 集成

效果链的核心技术亮点是 **`[SerializeReference]` 驱动的多态序列化**。每个步骤（SelectorStep、ConditionStep、EffectStep）都是抽象基类 `ChainStep` 的子类，Inspector 中可以直接通过 "managed reference" 下拉菜单选择具体子类创建实例，无需为每个步骤类型创建独立的 ScriptableObject。`ChainStepDrawer`（自定义 PropertyDrawer）使用反射自动枚举所有 `ChainStep` 子类，新增步骤类型后无需修改编辑器代码即可自动出现。

#### 6.2 链中断机制

每条效果链在执行过程中维护一个 `EffectContext` 实例，在步骤之间传递状态：

| 字段 | 说明 |
|---|---|
| `sourceCard` | 当前卡牌 |
| `executor` | 当前执行者 |
| `executed` | 当前被执行者 |
| `cachedPath` | 移动选择时缓存的路径 |
| `aiSelector` | AI 模式下的自动目标选择函数 |
| `chainBroken` | 条件失败或目标为空时中断链 |

`chainBroken` 是链中断的核心标志：
- `SelectorStep` 未找到目标 → `chainBroken = true`，跳过本链后续步骤
- `ConditionStep` 条件不满足 → `chainBroken = true`
- 当前链中断**不影响下一条链**的执行，每条链拥有独立的 `EffectContext`

#### 6.3 选择器与目标分离架构

选择逻辑与目标数据通过 `TargetSelector` 和 `ITarget` 两个抽象层分离：

```text
TargetSelector（选择逻辑）                    ITarget（目标数据）
├─ UnitSelector         ── 选出 →            ├─ UnitTarget
├─ CellAreaSelector     ── 选出 →            ├─ CellTarget
├─ CellPathSelector     ── 选出 →            └─ ...
├─ AllEnemySelector
└─ ...
```

`TargetSelector` 负责选择逻辑（筛选范围、条件过滤），返回 `List<ITarget>` 候选列表。`ITarget` 是统一接口，封装了不同目标类型（单位、格子等）的共性操作。选择器返回单个候选时 `AsyncEffectExecutor` 会自动确认，返回多个候选时交给 `PreviewManager` 等待玩家选择。

#### 6.4 异步执行与动画同步

带动画的效果实现 `IAnimatedEffect` 接口（提供 `PlayAnimation` 协程）。`AsyncEffectExecutor` 按以下流程执行：

```text
按卡牌顺序遍历 EffectChain
  ├─ 创建 EffectContext
  ├─ 执行 SelectorStep（获取候选目标）
  ├─ 执行 ConditionStep（检查条件）
  ├─ 执行 EffectStep（调用效果，若实现 IAnimatedEffect 则等待动画完成）
  └─ 链完成 → 下一条链
```

`AsyncEffectExecutor` 使用 `AwaitCompletion` 等待 `IAnimatedEffect` 的动画协程结束后再继续执行下一步，确保视觉效果与逻辑顺序一致。

#### 6.5 内置效果

内置效果包括伤害、移动、交换位置、治疗、自伤、加 Buff、获得移动点等。`TestAnimatedDelayEffect` 为占位测试效果（待实现）。

### 7. 单位系统

单位由 `UnitConfig` 生成，运行时由 `Unit` 表示逻辑状态，由 `UnitAppearance` 负责动画、朝向、血条和死亡表现。

#### 7.1 核心属性

| 属性 | 说明 |
|---|---|
| `currentHealth` / `maxHealth` | 当前生命与生命上限 |
| `attack` / `intelligence` | 攻击与智力 |
| `physicalDefense` / `magicDefense` | 物理防御与魔法防御 |
| `movePointLimit` / `movePoints` | 移动力上限与当前移动力 |
| `hasMoved` | 当前回合已移动步数 |

#### 7.2 ModifierManager 修饰器系统

`Unit` 持有 `ModifierManager` 实例，负责管理属性的运行时修正值。基础属性（attack、defense 等）与修饰器分离，修饰器可以叠加/移除而不影响基础值。伤害计算时，`DamageEffect` 从目标单位的 `ModifierManager` 读取物理/魔法防御修饰器参与减免计算。这套系统是实现 Buff 效果（如增加攻击力、降低防御）的底层基础，使属性修改与 Buff 生命周期解耦。

#### 7.3 Buff 栈策略

单位可以拥有先天 Buff 和运行时添加的 Buff。Buff 通过 `BuffContainer` 管理，实现了三种栈策略：

| 策略 | 行为 | 适用场景 |
|---|---|---|
| `Refresh` | 刷新 Buff 的剩余持续回合数 | 同效果持续时间重置 |
| `Overwrite` | 新 Buff 替换旧 Buff | 互斥 Buff（如变身为特定形态） |
| `Separate` | 独立叠加，各自计算 duration | 可多层叠加的 Buff（如增伤层数） |

`BuffContainer` 将 Buff 事件转发到 `Unit` 的核心生命周期钩子中（见下文），实现开闭原则——新增 Buff 效果不需要修改 `Unit` 核心逻辑。

#### 7.4 事件驱动的生命周期钩子

`Unit` 提供细粒度的事件钩子系统，供 Buff、修饰器和外部系统监听：

| 事件钩子 | 触发时机 |
|---|---|
| `OnBeforeDamage` | 伤害计算前，可用于修改伤害值（增伤/减伤） |
| `OnAfterDamage` | 伤害计算并应用后，可用于触发反伤、吸血等 |
| `OnMoved` | 单位移动完成后，可用于触发移动后效果 |

这些钩子通过 C# 事件（`event Action`）实现，`BuffContainer` 将 Buff 逻辑注入到这些钩子中，使 Buff 效果的扩展不需要修改 `Unit` 的代码。

### 8. AI 系统

AI 由数据和运行时状态共同驱动。`AIDeck` 定义每回合能量、策略和候选行为；`AIController` 在敌方回合逐个执行单位行动。

AI 行动评分考虑：

| 因素 | 说明 |
|---|---|
| `baseScore` | 行为基础分 |
| 距离 | 越靠近目标通常分数越高 |
| 自身血量 | 防御型 AI 更重视生存 |
| 目标血量 | 攻击型 AI 更偏好可击杀目标 |
| 能量效率 | 行为消耗与每回合能量的关系 |
| 冷却 | 冷却中的行为会受到惩罚 |
| 逃跑评分 | 低血量时可能选择远离敌人、靠近友军 |

AI 执行链时会向 `EffectContext` 注入 `aiSelector`，从候选目标中自动选择目标，不需要玩家交互。

### 9. UI 与输入系统

输入由 New Input System 驱动，`InputManager` 将单击、双击、右键、ESC 和长按转换成事件。UI 由 `UIManager` 管理面板显示、隐藏、遮罩转场和层级栈。

主要交互：

| 输入/按钮 | 作用 |
|---|---|
| 鼠标左键 | 点击单位、格子、卡牌，确认目标 |
| 鼠标双击 | 派发单位或格子双击事件 |
| 鼠标右键 | 派发格子右键事件 |
| ESC | 派发退出/返回事件 |
| 长按单位 | 显示单位信息 |
| 结束回合按钮 | 结束玩家出牌阶段，进入敌人阶段 |
| 暂停按钮 | 暂停游戏并打开暂停菜单 |

手牌 UI 采用对象池管理卡牌视觉，支持抽牌动画、弃牌动画、费用颜色、能量数字滚动和 pending 区动画。

#### 9.1 PreviewManager — PinBoard 目标选择机制

当效果链的 `SelectorStep` 返回多个候选目标时，`PreviewManager` 接管交互，采用 **"先钉选（Pin），再确认"** 的选择模式：

1. **高亮候选** — 系统高亮所有合法候选目标（单位或格子）
2. **悬停预览** — 鼠标悬停在候选上时，显示路径预览和效果预估
3. **钉选（Pin）** — 首次点击候选目标将其钉选（临时标记），但不立即确认
4. **确认** — 再次点击已钉选的目标，或满足选择数量后自动确认
5. **撤回** — 右键或 ESC 可撤回已固定的钉选，重新选择
6. **路径显示** — 移动类选择器会在钉选后显示完整路径

这种设计让玩家在确认前可以反复比较不同目标的利弊，降低误操作概率。当多个选择器链式执行时，`PreviewManager` 分阶段处理每个选择器的候选，按顺序逐步引导玩家完成全部选择。

AI 模式下，`AIController` 向 `EffectContext` 注入 `aiSelector` 函数替代玩家交互，`AsyncEffectExecutor` 检测到 `aiSelector` 存在时会直接调用它从候选列表中自动选择，完全跳过 PreviewManager。

### 10. 资源与存档系统

`ResourceManager` 管理能量、金币和牌库地址列表。`SaveManager` 负责 PlayerPrefs 设置和 JSON 存档。复杂运行状态集中在 `RunState` 中，包括玩家阵容、金币、能量上限、卡牌地址、全局关卡索引和随机种子。

存档文件默认写入游戏可执行文件同级的 `Saves` 文件夹，演示运行中主要使用 `run.json`。

## 七、编辑器工具设计

### 7.1 完整资产管线

项目最核心的编辑器能力是从 Tilemap 场景到运行时 ScriptableObject 资产的**一键提取管线**。设计师在场景中绘制不同 Tilemap 层，然后通过 `Tools -> Extract LevelData From Scene` 触发提取：

```text
Tilemap 场景（设计师可视化编辑）
│
├─ Base 层（基础地形）
├─ PlayerSpawn 层（玩家出生点）
├─ Goal 层（目标点）
├─ WinCondition 层（胜利条件标记）
├─ UnitSpawn 层（敌方单位出生点）
├─ CellChange 层（地形变化标记）
└─ RoundX 层（第X回合预设行动）
  │
  └─ LevelDataMenuExtractor 一键提取
       ├─ LevelGridData        ← 解析 Base / CellChange 层
       ├─ LevelTurnData        ← 解析 RoundX 层为 TurnAction 列表
       ├─ playerSpawnPositions ← 解析 PlayerSpawn 层
       ├─ goalPositions        ← 解析 Goal 层
       ├─ rootCondition        ← 解析 WinCondition 层为条件树
       └─ 自动注册 Addressables
            │
            └─ 运行时 LevelManager 加载
```

`WinCondition` Tilemap 层上的标记会被 `LevelDataMenuExtractor` 自动解析为 `VictoryCondition` 组合树（支持嵌套 AND/OR）。`RoundX` 层的 Tile 会被解析为 `SpawnUnitAction`、`CellChangeAction` 或 `EffectApplyAction`，实现可视化编辑回合事件。

### 7.2 工具列表

| 工具 | 说明 |
|---|---|
| `LevelDataMenuExtractor` | 从 Tilemap 场景提取关卡数据，自动解析 WinCondition 和 RoundX 层 |
| `CardDataEditor` | 卡牌颜色预设和卡牌数据编辑增强 |
| `AIDeckEditor` | AI 行为链条目编辑和预设按钮，支持保存/加载自定义预设 |
| `ChainStepDrawer` | 效果链步骤的多态 Inspector 绘制，反射枚举所有子类 |
| `ScriptableObjectIconDrawer` | ScriptableObject 图标显示 |
| `SolidColorSpriteGeneratorWindow` | 生成纯色和圆角辉光精灵 |

## 八、关键函数说明

| 函数 | 说明 |
|---|---|
| `GameManager.StartNewGame()` | 创建新 Run，加载地图场景 |
| `GameManager.LoadLevelAsync()` | 加载关卡场景和关卡数据 |
| `LevelManager.Initialize()` | 初始化棋盘、回合、单位、牌库和胜利条件 |
| `TurnManager.ChangePhase()` | 切换回合阶段并派发阶段变化事件 |
| `DeckManager.DrawCardsAsync()` | 抽牌并播放群组动画 |
| `DeckManager.CompleteCard()` | 根据卡牌去向放入弃牌堆、销毁堆或返回手牌 |
| `AsyncEffectExecutor.ExecuteCardChainsAsync()` | 执行卡牌全部效果链 |
| `GridManager.FindPath()` | 棋盘 BFS 寻路 |
| `GridManager.GetReachableCells()` | 获取指定步数内可达格 |
| `Unit.TakeDamage()` | 扣血、触发死亡、更新血条和事件 |
| `Unit.MoveTo()` | 逐格移动并触发移动事件 |
| `AIController.ExecuteTurn()` | 执行单个敌方单位的 AI 回合 |
| `TurnActionExecutor.ExecuteAll()` | 执行当前回合预设行动 |
| `VictoryChecker.Initialize()` | 初始化并开始监听胜利条件 |

## 九、可拓展性分析

| 拓展方向 | 现有支持方式 |
|---|---|
| 新卡牌 | 创建 `CardData`，组合选择器、条件和效果 |
| 新效果 | 继承 `Effect`，必要时实现 `IAnimatedEffect` |
| 新选择器 | 继承 `TargetSelector`，返回候选 `ITarget` |
| 新单位 | 创建 `UnitConfig` 和单位预制体 |
| 新敌人 AI | 创建 `AIDeck`，配置多条 `AIChainEntry` |
| 新关卡 | Tilemap 绘制后提取 `LevelData` |
| 新胜利条件 | 继承 `VictoryCondition`，必要时增加对应 Tile |
| 新地形 | 扩展 `TerrainType` 与 `TerrainConfig` |
| 新 Buff | 实现 `Buff` 子类，选择栈策略（Refresh/Overwrite/Separate），利用 `ModifierManager` 修改属性 |
| 新修饰器 | 通过 `ModifierManager` 添加新的修饰字段，参与伤害或属性计算 |
| 新 Target 类型 | 实现 `ITarget` 接口，配合对应的 `TargetSelector` |
| 新回合预设行动 | 继承 `TurnAction`，在 `LevelDataMenuExtractor` 中增加对应的 Tile 解析 |
| 难度曲线 | 配置 `RunConfig` 和 `SpawnGroup` 权重 |

## 十、当前不足与后续计划

1. `ContinueGame()`、`GameOver()` 等流程仍有待补全。
2. 地图系统与难度曲线已具备接口，但部分运行时接入仍可继续完善。
3. `EffectApplyAction` 当前直接应用单个效果，后续可改为完整效果链。
4. 单位跨关血量、经验、升级等长期成长数据已有 `RunState` 字段，但战斗结算逻辑仍需扩展。
5. UI 中背包、图鉴等入口已有预留，功能内容可继续补充。
6. 已知实现 Bug（需修复）：
   - `DamageEffect` 中魔法防御修饰器从施法方（`executor`）获取，应从受击方（`executed`）获取，导致魔法伤害减免计算错误。
   - `Unit.GetAttackPositionFromTarget()` 中的前/侧/背方位判定逻辑存在表达式错误，影响依赖攻击方位的 Buff（如背刺）的正确触发。

## 十一、总结

CardChess 的核心价值在于用统一的效果链表达卡牌、技能和 AI 行为，用 Tilemap 到 ScriptableObject 的管线表达关卡，用事件系统连接输入、UI、回合、单位和棋盘。这样的结构使玩法内容可以主要通过资产配置扩展，代码层则专注于通用规则、执行流程和表现同步。
