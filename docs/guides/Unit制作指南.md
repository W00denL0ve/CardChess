# Unit 制作指南

> 最后更新：2026-06-16 | 作者：WoodenLove

## 一、概述

> 本文档面向**游戏策划/美术**，提供 Unit 从资源准备到场景配置的完整制作流程。通过本指南，您将能够创建可用的单位配置和预制体，并配置敌人 AI。

## 二、前置条件

- 已准备好单位的 Sprite/模型资源和动画
- 熟悉 Unity 预制体编辑和 Animator 基本操作

## 三、操作步骤

### 1. 准备 Sprite 资源

| 素材 | 格式 | 用途 |
|------|------|------|
| 单位立绘/图标 | PNG/Sprite | UnitConfig 的 icon、卡牌 artwork |
| 单位模型/精灵图 | FBX/PSB | 场景中的 Unit 预制体 |

导入设置：Sprite Mode → Multiple（序列帧），Pixels Per Unit → 100，Filter Mode 按美术风格选择。
   - `Filter Mode` → `Point (no filter)`（像素风）或 `Bilinear`
   - 点击 `Sprite Editor` 切割（如果需要）

---

### 2. 创建 UnitConfig

### 操作步骤

1. 在 Project 窗口右键 → `Create` → `CardChess/Units/UnitConfig`
2. 命名，例如 `Warrior.asset`
3. 在 Inspector 中填写：

| 字段 | 说明 | 示例 |
|------|------|------|
| `Unit Id` | 唯一标识（建议与文件名一致） | `Warrior` |
| `Unit Name` | 显示名称 | `战士` |
| `Occupation` | 职业（影响动画选择） | `Warrior` |
| `Default Faction` | 默认阵营 | `Enemy` |
| `Icon` | 立绘 Sprite | 拖入 |
| `Unit Prefab` | 场景预制体 | 拖入 |
| `AI Deck` | AI 牌库（敌人必需） | 拖入（见第 6 节） |
| `Initial Attributes` | 初始属性列表 | 见下表 |
| `Innate Buffs` | 先天 Buff | 可选 |

### 初始属性配置

| 属性 | 类型 | 示例值 | 说明 |
|------|------|--------|------|
| `MaxHealth` | `Add` | 30 | 生命上限 |
| `Health` | `Add` | 30 | 当前生命（与 MaxHealth 一致） |
| `Attack` | `Add` | 5 | 基础攻击力 |
| `PhysicalDefense` | `Add` | 2 | 物理防御 |
| `MovePointLimit` | `Add` | 3 | 每回合移动力上限 |
| `MovePoints` | `Add` | 3 | 当前移动力 |

> **注意**：`Health` 和 `MaxHealth` 要设为相同值，否则开局血量不满。

### 坐标偏移

| 字段 | 说明 | 常用值 |
|------|------|--------|
| `Y Offset` | 垂直偏移 | 0~1（视模型高度） |
| `Z Offset` | 深度偏移 | -0.3 |
| `X Rotation` | X 轴旋转 | 45（俯视角） |

---

### 3. 设置 Unit 预制体

### 预制体结构

```
UnitPrefab (GameObject)
├── Unit (脚本)
│   ├── UnitId / Occupation / Faction
│   └── Health Bar → Slider（见第 5 节）
├── UnitAppearance (脚本)
│   ├── Animator Trigger 名称
│   └── Move Speed / Curve
├── Model/ (子物体，含 SkinnedMeshRenderer / SpriteRenderer)
│   └── Animator (挂 Animation Controller)
└── Canvas (World Space)
    └── HealthBar (Slider)
```

**最佳实践: 从Assets/Prefabs/Units/UnitPrefab制作变体**

### 脚本组件

**Unit.cs**
- `Unit Id` → 填 UnitConfig 中的 ID
- `Occupation` → 对应职业
- `Health Bar` → 拖入 Slider 组件

**UnitAppearance.cs**
- `Move Speed` → 移动动画速度
- `Animator Trigger` 名称要与 Animator Controller 一致：
  - `Walk` → 行走
  - `Idle` → 待机
  - `Attack` → 攻击
  - `Hit` → 受击
  - `Dead` → 死亡
  - `Teleport` → 传送（可选）

---

### 4. 配置 Animator

### 必要状态

| 状态名称 | Trigger 参数 | Blend 时间 | 说明 |
|----------|-------------|------------|------|
| `Idle` | `Idle` | 0.1s | 默认待机循环 |
| `Walk` | `Walk` | 0.1s | 移动循环 |
| `Attack` | `Attack` | 0.05s | 攻击单次，需设 AnimationEvent |
| `Hit` | `Hit` | 0.05s | 受击单次 |
| `Dead` | `Dead` | 0.1s | 死亡单次 |

### AnimationEvent 设置（攻击动画关键！）

在 Attack 动画剪辑的**击打帧**（如 0.3s 处）添加 AnimationEvent：

1. 在 Animation 窗口打开 Attack 动画
2. 时间轴移到挥击命中的帧
3. 添加 Event → 选择函数 `OnHitFrame`
4. **必须添加**，否则不触发伤害

### 状态机连线规则

```
Any State ──(Dead Trigger)──→ Dead → (end) → Idle
Idle ──(Attack Trigger)──→ Attack → (end) → Idle
Idle ──(Hit Trigger)──→ Hit → (end) → Idle
Idle ──(Walk Trigger)──→ Walk → (Idle Trigger) → Idle
```

> 所有 Trigger 用完后自动回到 Idle，用 `Has Exit Time` 控制过渡时机。

---

### 5. 配置血条

### 步骤

1. 在 Unit 预制体下创建 Canvas（Render Mode = World Space）
2. 在 Canvas 下创建 Slider（`UI → Slider`）
3. 调整 Slider 位置到单位头顶
4. 设置 Slider 样式：
   - 去掉 Handle（禁用）
   - `Fill Area` 的 Fill Image 设为红色渐变
   - `Background` 设为灰色
5. 将 Slider 组件拖入 Unit 脚本的 `Health Bar` 字段

### 血条自动行为

- 初始化时设为满血
- 受伤自动更新
- 治疗自动更新

---

### 6. 配置 AI（敌人专属）

### 创建 AIDeck

1. 右键 → `Create` → `CardChess/AI/AIDeck`
2. 命名，例如 `Warrior_AI.asset`

### 添加 AIChainEntry

每条 `AIChainEntry` 是一个可执行的技能/行为：

| 字段 | 说明 | 示例 |
|------|------|------|
| `Chain` | 效果链（与 CardData 的 chain 结构相同） | 选择器 → 伤害效果 |
| `Priority` | 优先级（越高越优先执行） | 10 |
| `Cooldown` | 使用后冷却回合数 | 2 |
| `Min Range` | 最小目标距离 | 1 |
| `Max Range` | 最大目标距离 | 3 |
| `HP Threshold` | 血量阈值（低于此值时优先使用） | 0.3 |
| `Max Use Per Battle` | 每场战斗最大使用次数 | 0 = 无限 |

### 将 AIDeck 挂到 UnitConfig

在 UnitConfig 的 `AI Deck` 字段拖入刚才创建的 AIDeck。

---

### 7. 注册到关卡

### 在 SpawnGroup 中引用

1. 右键 → `Create` → `CardChess/Units/SpawnGroup`
2. 在 `Unit Entries` 列表中：
   - `Config` → 拖入 UnitConfig
   - `Count` → 生成数量
   - `Faction Override` → 阵营覆盖（可选）

### 关卡的 LevelData 引用 SpawnGroup

在 `LevelData` 或 `LevelTurnData` 中指定 SpawnGroup 与生成位置。

## 四、配置参考

### UnitConfig 字段

| 字段 | 说明 | 示例 |
|------|------|------|
| `Unit Id` | 唯一标识 | `Warrior` |
| `Unit Name` | 显示名称 | `战士` |
| `Occupation` | 职业 | `Warrior` |
| `Default Faction` | 默认阵营 | `Enemy` |
| `Icon` | 立绘 Sprite | 拖入 |
| `Unit Prefab` | 场景预制体 | 拖入 |
| `AI Deck` | AI 牌库 | 敌方必需 |
| `Initial Attributes` | 初始属性 | |
| `Innate Buffs` | 先天 Buff | 可选 |

### 初始属性

| 属性 | 示例值 | 说明 |
|------|--------|------|
| `MaxHealth` | 30 | 生命上限 |
| `Health` | 30 | 当前生命（应与 MaxHealth 一致） |
| `Attack` | 5 | 基础攻击力 |
| `PhysicalDefense` | 2 | 物理防御 |
| `MovePointLimit` | 3 | 每回合移动力上限 |
| `MovePoints` | 3 | 当前移动力 |

### Animator 必要状态

| 状态 | Trigger 参数 | 说明 |
|------|-------------|------|
| Idle | `Idle` | 默认待机循环 |
| Walk | `Walk` | 移动循环 |
| Attack | `Attack` | 攻击单次，需设 AnimationEvent |
| Hit | `Hit` | 受击单次 |
| Dead | `Dead` | 死亡单次 |

> 攻击动画必须在命中帧添加 `OnHitFrame` AnimationEvent，否则不触发伤害。

## 五、常见问题

**问题：单位生成后血量为 0**
**解决：** 检查 `UnitConfig` 中 `Health` 和 `MaxHealth` 是否设为相同值。

**问题：攻击不造成伤害**
**解决：** 检查 Attack 动画是否添加了 `OnHitFrame` AnimationEvent。

**问题：敌人不行动**
**解决：** 检查 `UnitConfig.aiDeck` 是否配置、`AIDeck.entries` 是否有条目。

## 六、相关文档

- [卡牌制作指南](卡牌制作指南.md) — 卡牌与效果链配置
- [地图制作指南](地图制作指南.md) — 关卡编辑与 Tilemap 使用
- [架构设计文档](../design/CardChess设计文档.md) — 设计总纲与架构决策
