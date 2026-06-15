# CardChess 简明使用文档

## 一、运行项目

### 1. 打开工程

1. 使用 Unity 2022.3.30f1c1 打开项目根目录。
2. 等待 Unity 导入依赖和资源。
3. 推荐从 `Assets/Scenes/Boot.unity` 启动游戏。

### 2. 开始游戏

1. 打开 `Boot` 场景并点击 Play。
2. 等待启动流程加载到主菜单。
3. 点击主菜单的开始按钮。
4. 系统会进入地图场景并生成地图。
5. 在地图中选择或触发关卡入口后进入战斗关卡。

### 3. 运行时主要流程

```text
主菜单 -> 开始游戏 -> 地图 -> 进入关卡 -> 抽牌 -> 出牌 -> 敌方行动 -> 下一回合
```

## 二、基础操作

| 操作 | 功能 |
|---|---|
| 鼠标左键点击卡牌 | 打出手牌 |
| 鼠标左键点击单位 | 选择单位目标 |
| 鼠标左键点击格子 | 选择格子目标或移动目标 |
| 鼠标双击单位/格子 | 触发双击事件，供后续交互使用 |
| 鼠标右键点击格子 | 触发右键事件 |
| 长按单位 | 显示单位信息 |
| ESC | 触发返回/关闭类事件 |
| 结束回合按钮 | 结束玩家出牌阶段，进入敌方回合 |
| 暂停按钮 | 暂停游戏并打开暂停菜单 |
| 地图按钮 | 在关卡中显示或隐藏地图面板 |

## 三、战斗规则速览

1. 每回合开始时会先执行关卡预设行动，例如刷怪或改变地形。
2. 抽牌阶段会恢复能量并抽取固定数量卡牌。
3. 玩家出牌阶段可以点击手牌出牌。
4. 卡牌能量不足时不能打出，能量显示会出现警告反馈。
5. 打出卡牌后，根据卡牌效果链选择目标并执行效果。
6. 点击结束回合后，未保留的手牌会被弃掉。
7. 敌方单位按 AI 配置逐个行动。
8. 满足胜利条件后关卡结束。

## 四、常见内容制作

### 1. 制作卡牌

1. 在 Project 窗口右键选择 `Create -> CardChess -> Cards -> CardData`。
2. 填写卡牌名称、描述、消耗、打出后去向、是否保留和颜色。
3. 在 `Chains` 中添加效果链。
4. 常见链结构为：

```text
SelectorStep -> ConditionStep -> EffectStep
```

示例：单体攻击卡

```text
Chain 0
  SelectorStep: 选择敌方单位
  EffectStep: DamageEffect
```

5. 将卡牌加入 `GameStartConfig.initialCards`，即可在新游戏初始牌库中使用。

详细做法见 `卡牌制作指南.md`。

### 2. 制作单位

1. 准备单位模型或 Sprite、图标和动画。
2. 右键选择 `Create -> CardChess -> Units -> UnitConfig`。
3. 设置 `unitId`、显示名称、职业、默认阵营、图标、单位预制体和初始属性。
4. 敌方单位需要配置 `AI Deck`。
5. 将单位配置设为 Addressable，地址要和开局阵容或刷怪配置中的字符串一致。

详细做法见 `Unit制作指南.md`。

### 3. 配置敌人 AI

1. 右键选择 `Create -> CardChess -> AI -> AIDeck`。
2. 设置每回合能量和策略类型。
3. 在 `entries` 中添加 `AIChainEntry`。
4. 为每个条目配置效果链、能量消耗、冷却、目标类型、行为类别和基础分。
5. 将该 `AIDeck` 拖入敌方 `UnitConfig`。

### 4. 制作关卡地图

1. 新建或复制一个关卡编辑场景。
2. 在 Grid 下创建并命名 Tilemap 层：

| Tilemap 层名 | 用途 |
|---|---|
| `Base` | 基础地形 |
| `PlayerSpawn` | 玩家出生点 |
| `Goal` | 目标点 |
| `WinCondition` | 胜利条件 |
| `Round1`、`Round2` 等 | 指定回合行动 |

3. 在 `Base` 层绘制地形 Tile。
4. 在 `PlayerSpawn` 层绘制玩家出生点。
5. 在 `WinCondition` 层绘制胜利条件 Tile。
6. 在 `RoundX` 层绘制刷怪或格子变化 Tile。
7. 保存场景。
8. 点击菜单 `Tools -> Extract LevelData From Scene`。
9. 检查生成的 `LevelData`、`GridData` 和 `TurnData` 资产。
10. 将关卡场景和 `LevelData` 配置为 Addressable，地址通常与场景名一致。

详细做法见 `地图制作指南.md`。

### 5. 设置开局配置

开局配置资产位于：

```text
Assets/ScriptableObjects/Configs/GameStartConfig.asset
```

常用字段：

| 字段 | 说明 |
|---|---|
| `initialRoster` | 初始角色的 UnitConfig Addressable 地址 |
| `initialCards` | 初始卡牌列表 |

如果进入关卡后没有玩家单位，优先检查 `initialRoster` 地址是否能加载到对应 `UnitConfig`。

## 五、常见问题

### 1. 进入关卡后没有手牌

检查 `GameStartConfig.initialCards` 是否为空。`DeckManager.Initialize()` 会直接使用这个列表作为初始牌库。

### 2. 点击卡牌没有效果

可能原因：

| 原因 | 处理 |
|---|---|
| 当前不在 PlayerPlay 阶段 | 等待抽牌完成或效果执行完成 |
| 能量不足 | 结束回合或调整卡牌费用 |
| 卡牌没有效果链 | 在 `CardData.chains` 中添加步骤 |
| 选择器没有候选目标 | 检查目标阵营、范围、单位是否存活 |

### 3. 进入关卡后没有玩家单位

检查以下内容：

1. `LevelData.playerSpawnPositions` 是否有出生点。
2. `GameStartConfig.initialRoster` 是否填写了正确的 UnitConfig 地址。
3. 对应 `UnitConfig` 是否设为 Addressable。
4. 单位预制体是否挂有 `Unit`、`UnitAppearance` 等必要组件。

### 4. 敌人不行动

检查以下内容：

1. 敌方 `UnitConfig.aiDeck` 是否为空。
2. `AIDeck.entries` 是否为空。
3. AI 效果链是否有可用目标。
4. 敌人是否属于 `Faction.Enemy`。

### 5. 地图提取后运行时格子异常

检查以下内容：

1. `Base` Tilemap 是否覆盖了完整棋盘。
2. Tilemap 层命名是否符合约定。
3. 地形 Tile 是否使用 `TerrainTile`。
4. 提取后 `LevelData.gridData` 和 `turnData` 引用是否正确。
5. 关卡加载的 Addressable Key 是否与 `GameManager.LoadLevelAsync()` 使用的一致。

### 6. 胜利条件不触发

检查以下内容：

1. 场景中是否存在 `VictoryChecker`。
2. `LevelData.rootCondition` 是否为空。
3. 如果使用到达目标点条件，`Goal` 层是否提取出了目标点。
4. 如果使用保护单位条件，目标单位 ID 是否手动填写正确。

## 六、常用目录

| 目录 | 内容 |
|---|---|
| `Assets/Scripts/Core` | 启动、事件、输入等核心基础设施 |
| `Assets/Scripts/Game/Card` | 卡牌数据、牌库和卡牌表现 |
| `Assets/Scripts/Game/Effect` | 效果链、选择器、条件、效果 |
| `Assets/Scripts/Game/Unit` | 单位、属性、外观、单位工厂 |
| `Assets/Scripts/Game/Grid` | 棋盘数据和运行时格子 |
| `Assets/Scripts/Game/Turn` | 回合状态机和回合行动 |
| `Assets/Scripts/Game/AI` | 敌人 AI 配置与执行 |
| `Assets/Scripts/Game/Level` | 关卡数据和胜利条件 |
| `Assets/Scripts/Game/UI` | UI 面板、手牌 UI、HUD |
| `Assets/ScriptableObjects` | 卡牌、单位、关卡、地形、配置等资产 |
| `Assets/Editor` | 关卡提取和自定义 Inspector 工具 |

## 七、推荐调试顺序

当一个新关卡或新卡牌无法正常工作时，建议按这个顺序检查：

1. 关卡是否从 `Boot` 场景正常进入。
2. Console 是否有 Addressables 加载失败。
3. `LevelManager.Initialize()` 是否完成。
4. `GridManager` 是否加载了 `LevelGridData`。
5. 玩家单位是否生成在出生点。
6. `DeckManager` 是否拿到了初始牌库。
7. 卡牌效果链是否能找到目标。
8. 胜利条件是否已初始化。

## 八、参考文档

项目根目录下已有更细的专题文档：

| 文档 | 内容 |
|---|---|
| `卡牌制作指南.md` | 卡牌、效果链、选择器、效果配置 |
| `Unit制作指南.md` | 单位配置、预制体、动画和 AI |
| `地图制作指南.md` | Tilemap 关卡绘制与数据提取 |
| `敌人AI执行链路.md` | AI 决策和执行流程 |
| `GameEvent用法详解.md` | 事件系统使用方式 |
| `数据与表现分离设计.md` | 数据层、编辑层、运行层分离思路 |
| `胜利条件判断架构分析.md` | 胜利条件系统 |
| `难度曲线系统用法与可拓展性分析.md` | SpawnGroup 与难度曲线 |
