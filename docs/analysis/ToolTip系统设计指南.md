# ToolTip 系统设计指南

## 概述

ToolTip 系统用于鼠标悬停时显示上下文信息。与 `ILongPressTarget`（长按查看详细面板）互补，ToolTip 提供**轻量、即时**的信息预览。

## 架构

```
InputManager（悬停检测）
  → ToolTipDetector（检测鼠标下的 ToolTip 组件）
    → C# 事件通知 ToolTipController
      → ToolTipController（UI 层）
          → 调用 ToolTip.BuildContent(IToolTipBuilder)
          → 定位到鼠标位置并显示
```

> ToolTip 只有 Detector → Controller **一对一**通信，无需全局事件总线，直接使用 C# 事件。

## 组件分工

### 1. `ToolTip`（MonoBehaviour）— 挂载在需要提示的对象上

```csharp
/// <summary>
/// ToolTip 提供者 — 挂载在需要显示提示的对象上，自动创建 Trigger Collider。
/// 子类可重写 BuildContent 构建自定义内容。
/// </summary>
public class ToolTip : MonoBehaviour
{
    [Header("基本内容")]
    [SerializeField] private string title;
    [SerializeField][TextArea] private string description;

    [Header("Collider 自动创建")]
    [SerializeField] private Vector3 colliderSize = new Vector3(1, 1, 1);
    [SerializeField] private bool isTrigger = true;

    private void Awake()
    {
        SetupCollider();
    }

    /// <summary>自动创建/获取 Trigger Collider</summary>
    private void SetupCollider()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            ((BoxCollider)col).size = colliderSize;
        }
        col.isTrigger = isTrigger;
    }

    /// <summary>虚方法，子类可重写构建更复杂的提示内容</summary>
    public virtual void BuildContent(IToolTipBuilder builder)
    {
        if (!string.IsNullOrEmpty(title))
            builder.SetTitle(title);
        if (!string.IsNullOrEmpty(description))
            builder.SetDescription(description);
    }
}
```

### 2. `IToolTipBuilder` — 构建器接口

由 `ToolTipController` 实现并传入，ToolTip 组件通过它填充内容：

```csharp
public interface IToolTipBuilder
{
    void SetTitle(string text);
    void SetDescription(string text);
    void SetIcon(Sprite icon);
    void AddStatRow(string label, string value);      // 属性行：攻击 12
    void AddProgressBar(float fill, float max, string label);  // 进度条
    void AddDivider();                                  // 分隔线
    void AddCustomContent(GameObject prefab);           // 自定义预制体
}
```

### 3. `ToolTipController`（UI/Widget）— 悬挂在 SystemLayer 下

```csharp
/// <summary>
/// ToolTip UI 控制器 — 监听 ToolTipShowEvent/ToolTipHideEvent，
/// 构建并显示 ToolTip 窗口，跟随鼠标位置。
/// </summary>
public class ToolTipController : MonoBehaviour, IToolTipBuilder
{
    [Header("ToolTip UI 根")]
    [SerializeField] private GameObject toolTipRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform statContainer;
    [SerializeField] private GameObject statRowPrefab;
    [SerializeField] private Transform progressContainer;
    [SerializeField] private GameObject progressBarPrefab;

    [Header("位置偏移")]
    [SerializeField] private Vector2 offset = new Vector2(15, -15);

    [Header("延迟")]
    [SerializeField] private float showDelay = 0.5f;   // 悬停 0.5s 后才显示

    private ToolTipDetector detector;
    private Coroutine delayCoroutine;

    private void Start()
    {
        detector = FindObjectOfType<ToolTipDetector>();
        if (detector != null)
        {
            detector.OnShow += OnShow;
            detector.OnHide += OnHide;
        }
    }

    private void OnDestroy()
    {
        if (detector != null)
        {
            detector.OnShow -= OnShow;
            detector.OnHide -= OnHide;
        }
    }

    private void OnShow(ToolTip provider)
    {
        // 延迟显示，避免划过时闪烁
        delayCoroutine = StartCoroutine(DelayedShow(provider));
    }

    private IEnumerator DelayedShow(ToolTip provider)
    {
        yield return new WaitForSeconds(showDelay);
        // 清空旧内容
        ClearContent();
        // 让 ToolTip 组件通过 builder 填充
        provider.BuildContent(this);
        // 定位到鼠标位置
        UpdatePosition(Mouse.current.position.ReadValue());
        toolTipRoot.SetActive(true);
    }

    private void OnHide()
    {
        if (delayCoroutine != null) StopCoroutine(delayCoroutine);
        toolTipRoot.SetActive(false);
    }

    // IToolTipBuilder 实现
    public void SetTitle(string text) { titleText.text = text; }
    public void SetDescription(string text) { descText.text = text; }
    // ...
}
```

### 4. `ToolTipDetector`（Input）— 检测悬停目标

```csharp
/// <summary>
/// ToolTip 检测器 — 在 InputManager 的 Update 中每帧检测。
/// 只检测 tooltipLayerMask 层上的 ToolTip 组件。
/// 通过 C# 事件通知 ToolTipController，不经过全局事件总线。
/// </summary>
public class ToolTipDetector : MonoBehaviour
{
    public event System.Action<ToolTip> OnShow;
    public event System.Action OnHide;

    [SerializeField] private LayerMask tooltipLayerMask;
    [SerializeField] private Camera mainCamera;

    private ToolTip currentTarget;

    private void Update()
    {
        ToolTip newTarget = DetectToolTip();
        if (newTarget != currentTarget)
        {
            if (currentTarget != null) OnHide?.Invoke();
            if (newTarget != null) OnShow?.Invoke(newTarget);
            currentTarget = newTarget;
        }
    }

    private ToolTip DetectToolTip()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tooltipLayerMask))
            return hit.collider.GetComponentInParent<ToolTip>();
        return null;
    }
}
```

## 使用示例

### 基础用法 — 静态物体描述

在任意 GameObject 上挂载 `ToolTip` 组件，Inspector 中填写标题和描述：

```
[GameObject: 宝箱]
└── ToolTip
    ├── Title: "古老的宝箱"
    └── Description: "里面似乎藏着什么东西..."
```

组件 `Awake` 时会自动创建 `BoxCollider`（Trigger），无需手动配置。

### Buff 图标 — 动态内容

```csharp
public class BuffToolTip : ToolTip
{
    private BuffInstance buff;

    public void Setup(BuffInstance buff)
    {
        this.buff = buff;
    }

    public override void BuildContent(IToolTipBuilder builder)
    {
        builder.SetTitle(buff.BuffData.buffId);
        builder.SetDescription(buff.BuffData.description);
        builder.SetIcon(buff.BuffData.icon);
        builder.AddStatRow("层数", buff.CurrentStacks.ToString());
        builder.AddProgressBar(buff.RemainingDuration, buff.RemainingDuration + 1, "剩余回合");
    }
}
```

### 单位 — 悬停摘要

```csharp
public class UnitToolTip : ToolTip
{
    private Unit unit;

    public void Setup(Unit unit) => this.unit = unit;

    public override void BuildContent(IToolTipBuilder builder)
    {
        builder.SetTitle(unit.UnitName);
        builder.SetIcon(unit.Icon);
        builder.AddStatRow("生命", $"{unit.baseValue.currentHealth}/{unit.baseValue.maxHealth}");
        builder.AddStatRow("攻击", unit.baseValue.attack.ToString());
        builder.AddDivider();
        builder.SetDescription("长按查看详细信息");
    }
}
```

## 与长按系统（ILongPressTarget）的关系

| | ToolTip | ILongPressTarget |
|---|---|---|
| 触发 | 鼠标悬停（0.5s 延迟） | 长按（Hold 交互） |
| 显示 | 轻量浮窗 | 全功能面板 |
| 生命周期 | 移出即消失 | 手动关闭 |
| 定位 | 跟随鼠标 | 固定在目标位置 |
| 数据源 | ToolTip 组件 | IPanelDataReceiver |

两者可以共存于同一对象上（如 Unit），互不冲突。
