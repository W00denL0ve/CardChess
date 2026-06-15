# 🎮 CardChess

**卡牌 + 战棋策略游戏** — 回合制对战，玩家操控己方单位，通过打出手牌触发移动、攻击、治疗、Buff 等效果链，与敌方单位战斗。

| 项目 | 内容 |
|---|---|
| 引擎 | Unity 2022.3.30f1c1 |
| 语言 | C# |
| 主要技术 | ScriptableObject、Addressables、Tilemap、New Input System、UGUI、TextMeshPro、DOTween |

---

## 🚀 快速开始

1. 打开 `Assets/Scenes/Boot.unity` — 启动场景
2. 点击 **Play** 运行游戏
3. 主菜单 → 新游戏 → 选择关卡 → 回合战斗

**场景说明**：

| 场景 | 作用 |
|---|---|
| `Boot` | 启动引导，实例化全局 Manager |
| `MainMenu` | 主菜单 |
| `Map` | 大地图，选择关卡 |
| `Levels/*` | 关卡战斗场景 |

---

## 🏛️ 架构概览

### 核心技术决策

- **效果链系统** — 卡牌技能、AI 行为统一用 `EffectChain`（SelectorStep → ConditionStep → EffectStep）表达，`[SerializeReference]` 多态序列化支撑 Inspector 直接配置
- **数据与表现分离** — ScriptableObject 存储静态配置，运行时管理器生成逻辑对象，表现层通过事件和协程同步
- **事件总线** — `GameEventChannel` 泛型事件驱动，系统间通过 `GameEvent` 派生类解耦
- **Tilemap 关卡管线** — 设计师在 Tilemap 可视化编辑，一键提取为 `LevelData` 运行时资产

### 场景流程

```text
Boot → MainMenu → Map → Level → 回合战斗 → 结算
```

### 回合状态机

```text
Start → Draw → PlayerPlay → PlayerAction → Enemy → End → 下一回合
```

六个阶段由 `TurnManager` 自动推进，`PlayerPlay` 阶段等待玩家出牌或点击结束回合。

### 项目结构

```
Assets/
├── Scripts/          # 源代码
│   ├── Core/         # 启动引导、事件总线、输入
│   └── Game/         # 业务逻辑 (AI/卡牌/单位/效果/网格/关卡/回合/UI/预览等)
├── Editor/           # 编辑器工具
├── ScriptableObjects/# 运行时数据资产
├── Scenes/           # 游戏场景
└── docs/             # 架构文档
```

> 完整文件结构见 [`docs/project/项目文件结构.md`](docs/project/项目文件结构.md)

---

## 📦 核心系统

| 系统 | 说明 | 关键脚本 |
|---|---|---|
| **事件系统** | 泛型事件总线，全局解耦 | `GameEventChannel` |
| **回合系统** | 六阶段状态机，自动推进 | `TurnManager` |
| **网格系统** | BFS 寻路、占用管理、坐标转换 | `GridManager` / `GridVisualizer` |
| **效果系统** | 链式架构，多态步骤，异步执行 | `AsyncEffectExecutor` / `EffectChain` |
| **单位系统** | 属性、修饰器、Buff、事件钩子 | `Unit` / `UnitAppearance` / `ModifierManager` / `BuffContainer` |
| **卡牌系统** | 数据资产 → 手牌 → 效果链执行 | `CardData` / `DeckManager` / `HandUI` |
| **AI 系统** | 评分决策，复用效果链 | `AIController` / `AIDeck` |
| **胜利条件** | AND/OR 组合条件树 | `VictoryChecker` / `CompositeCondition` |
| **预览系统** | PinBoard 钉选确认交互 | `PreviewManager` |
| **关卡系统** | Tilemap 提取→运行时加载 | `LevelDataMenuExtractor` / `LevelManager` |
| **资源存档** | 能量/金币管理，JSON 持久化 | `ResourceManager` / `SaveManager` / `RunState` |
| **Buff 系统** | 三种栈策略，事件驱动 | `BuffContainer` / `Buff` |
| **相机系统** | 俯角控制，目标聚焦 | `CameraController` |
| **输入系统** | New Input System 包装 | `InputManager` |
| **UI 系统** | 面板栈、转场遮罩、手牌 UI | `UIManager` |

> 各系统详细描述见 [`docs/design/系统模块详解.md`](docs/design/系统模块详解.md)

---

## 🛠️ 编辑器工具

| 工具 | 作用 |
|---|---|
| `LevelDataMenuExtractor` | Tilemap → ScriptableObject 一键提取 |
| `AIDeckEditor` | AI 配置编辑，预设保存/加载 |
| `CardDataEditor` | 卡牌颜色预设和编辑 |
| `ChainStepDrawer` | 效果链步骤多态 Inspector 绘制 |
| `SolidColorSpriteGeneratorWindow` | 纯色/圆角辉光精灵生成 |

---

## 📚 文档索引

| 文档 | 内容 |
|---|---|
| **设计** | |
| [`docs/design/CardChess设计文档.md`](docs/design/CardChess设计文档.md) | 设计总纲、架构决策、可拓展性分析 |
| [`docs/design/系统模块详解.md`](docs/design/系统模块详解.md) | 各系统详细技术说明 |
| [`docs/design/数据与表现分离设计.md`](docs/design/数据与表现分离设计.md) | 架构原则详解 |
| **指南** | |
| [`docs/guides/CardChess简明使用文档.md`](docs/guides/CardChess简明使用文档.md) | 快速上手 |
| [`docs/guides/地图制作指南.md`](docs/guides/地图制作指南.md) | 关卡编辑与 Tilemap 使用 |
| [`docs/guides/Unit制作指南.md`](docs/guides/Unit制作指南.md) | 单位制作流程 |
| [`docs/guides/卡牌制作指南.md`](docs/guides/卡牌制作指南.md) | 卡牌与效果链配置 |
| **分析** | |
| [`docs/analysis/GameEvent用法详解.md`](docs/analysis/GameEvent用法详解.md) | 事件系统使用指南 |
| [`docs/analysis/敌人AI执行链路.md`](docs/analysis/敌人AI执行链路.md) | AI 决策与执行链路 |
| [`docs/analysis/胜利条件判断架构分析.md`](docs/analysis/胜利条件判断架构分析.md) | 胜利条件系统 |
| [`docs/analysis/难度曲线系统用法与可拓展性分析.md`](docs/analysis/难度曲线系统用法与可拓展性分析.md) | 难度曲线配置 |
| [`docs/analysis/ToolTip系统设计指南.md`](docs/analysis/ToolTip系统设计指南.md) | ToolTip 系统 |
| [`docs/analysis/长按目标(ILongPressTarget)拓展指南.md`](docs/analysis/长按目标(ILongPressTarget)拓展指南.md) | 长按交互拓展 |
| **项目** | |
| [`docs/project/项目文件结构.md`](docs/project/项目文件结构.md) | 完整文件树 |

---



## 十七、架构文档

- `地图制作指南.md`：Tilemap 层命名、地形绘制、回合行动、胜利条件配置、提取流程
- `Unit制作指南.md`：Sprite → UnitConfig → 预制体 → Animator → AI
- `卡牌制作指南.md`：CardData 创建与效果链配置
- `数据与表现分离设计.md` / `GameEvent用法详解.md`

---

*更新日期：2026-05-22*
