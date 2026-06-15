# 长按目标（ILongPressTarget）拓展指南

## 概述

`ILongPressTarget` 接口定义了对所有可长按交互目标的通用行为。任何实现了此接口的对象都可以被长按检测系统识别，并触发对应的 UI 反馈和信息面板显示。

## 接口定义

```csharp
public interface ILongPressTarget
{
    /// <summary>获取该目标在屏幕上的位置（用于定位 UI）</summary>
    Vector3 GetScreenPosition();
    GameObject gameObject { get; }
}
```

---

## 新增一个可长按目标的步骤

### 第一步：让目标类实现接口

以 `Cell` 为例：

```csharp
// Cell.cs
public class Cell : MonoBehaviour, ILongPressTarget
{
    public Vector3 GetScreenPosition()
    {
        // 需要世界坐标 → 屏幕坐标的转换
        return Camera.main?.WorldToScreenPoint(transform.position) ?? Vector3.zero;
    }
    // gameObject 继承自 MonoBehaviour，无需额外实现
}
```

> 💡 **非 MonoBehaviour 类型**：如果目标不是 MonoBehaviour（例如纯数据类），需要用一个包装类来实现接口，参考 `UnitTarget` 的包装器模式。

### 第二步：在 InputManager 中添加检测逻辑

打开 `InputManager.cs`，找到 `GetLongPressTargetUnderMouse()` 方法，添加新的检测分支：

```csharp
ILongPressTarget GetLongPressTargetUnderMouse()
{
    // 优先检测单位
    Unit unit = GetUnitUnderMouse();
    if (unit != null) return unit;

    // 👇 在此处添加新的检测逻辑
    Cell cell = GetCellUnderMouse();
    if (cell != null) return cell;

    return null;
}
```

建议新增专用的检测方法（如 `GetCellUnderMouse()`），与原检测方法分离：

```csharp
Cell GetCellUnderMouse()
{
    if (mainCamera == null || cellLayerMask.value == 0) return null;

    Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
    if (Physics.Raycast(ray, out RaycastHit hit, 100f, cellLayerMask))
    {
        Cell cell = hit.collider.GetComponentInParent<Cell>();
        if (cell != null) return cell;
    }
    return null;
}
```

### 第三步：在 LongPressInfoDetector 中添加面板显示逻辑

打开 `LongPressInfoDetector.cs`，在 `OnLongPressPerformed` 中添加新分支：

```csharp
private void OnLongPressPerformed(LongPressPerformedEvent evt)
{
    if (evt.Target is Unit unit)
    {
        ShowInfoPanel("UnitInfoPanel", unit);
    }
    // 👇 在此处添加新的类型判断
    else if (evt.Target is Cell cell)
    {
        ShowInfoPanel("CellInfoPanel", cell);
    }
    else if (evt.Target is YourNewType yourObj)
    {
        ShowInfoPanel("YourPanelName", yourObj);
    }
}
```

> ⚠️ 确保面板名称已在 `UIManager` 的 `panelPrefabs` 列表中注册，并且对应的面板脚本实现了 `IPanelDataReceiver` 接口。

---

## 事件速查表

| 事件 | 触发时机 | 用途 |
|------|----------|------|
| `LongPressStartedEvent` | 按下瞬间（已检测到目标） | 显示环形 Slider、开始视觉反馈 |
| `LongPressUpdateEvent` | 每帧（长按持续中） | 更新环形 Slider 的 fillAmount |
| `LongPressCancelledEvent` | 松开按钮（未达到阈值） | 隐藏/销毁环形 Slider |
| `LongPressPerformedEvent` | 达到阈值（长按完成） | 显示信息面板 |

所有事件均携带 `ILongPressTarget Target` 属性，通过 `evt.Target` 获取目标引用。

---

## 完整链路示例

```
用户长按 Unit
  → InputManager.OnLongPressStarted()
    → 检测 GetLongPressTargetUnderMouse() → 返回 Unit（实现了 ILongPressTarget）
    → 派发 LongPressStartedEvent(unit)
      → UnitLongPressSliderController 收到 → 实例化环形 Slider 并定位到 unit.GetScreenPosition()
      → LongPressInfoDetector（跳过，不处理 started 事件）

长按持续中（每帧）
  → InputManager.Update() → 计算 progress → 派发 LongPressUpdateEvent(unit, progress)
    → UnitLongPressSliderController 收到 → 更新 Slider.fillAmount = progress

用户继续按住直到达到阈值（0.3s）
  → InputManager.OnLongPressPerformed()
    → 派发 LongPressPerformedEvent(unit)
      → UnitLongPressSliderController 收到 → 填满 Slider 后销毁
      → LongPressInfoDetector 收到 → 判断 target is Unit → UIManager.Show("UnitInfoPanel", data: unit)

用户松开
  → InputManager.OnLongPressCanceled()
    → 派发 LongPressCancelledEvent(unit)
    → pendingLongPressTarget = null
```
