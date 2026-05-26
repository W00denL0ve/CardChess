using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 面板层级类型
/// </summary>
public enum PanelType { HUD, Panel, Overlay }

/// <summary>
/// 全局 UI 管理器，负责面板的显示、隐藏、层级和遮罩。
/// 挂载在 Boot 场景的 Managers 子物体上。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    /// <summary>暴露主 Canvas 供外部组件使用</summary>
    public Canvas MainCanvas => mainCanvas;

    [Header("主画布")]
    [SerializeField] private Canvas mainCanvas;

    [Header("面板预制体注册")]
    [SerializeField] private List<PanelEntry> panelPrefabs = new List<PanelEntry>();

    [Header("背景遮罩")]
    [SerializeField] private GameObject backgroundMaskPrefab; // 背景遮罩预制体

    private Image maskImage; // 运行时实例化的背景遮罩 Image 组件

    [Header("转场遮罩")]
    [SerializeField] private GameObject Mask; // 初始化时实例化并隐藏
    
    private MaskRadiusAnimator maskRadiusAnimator; // 转场用的镂空遮罩动画组件
    private Coroutine panelSwitchCoroutine;        // 防止多个 DelayedPanelSwitch 竞态

    // 层级容器（运行时动态创建）
    private Transform backgroundLayer;
    private Transform panelLayer;
    private Transform overlayLayer;
    private Transform systemLayer;

    // 面板实例字典（按名称索引）
    private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
    // 面板栈，用于层级管理（最近打开的在上层）
    private Stack<GameObject> panelStack = new Stack<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 实例化主画布
        mainCanvas = Instantiate(mainCanvas);
        // 设置为跨场景保留
        DontDestroyOnLoad(mainCanvas.gameObject);

        // 动态创建层级容器（从下到上）
        backgroundLayer = CreateLayer("BackgroundLayer");
        panelLayer = CreateLayer("PanelLayer");
        overlayLayer = CreateLayer("OverlayLayer");
        systemLayer = CreateLayer("SystemLayer");

        // 实例化背景遮罩（放在 OverlayLayer）
        if (backgroundMaskPrefab != null)
        {
            GameObject maskObj = Instantiate(backgroundMaskPrefab, overlayLayer);
            maskObj.name = "BackgroundMask";
            maskObj.SetActive(false);
            maskObj.transform.SetAsFirstSibling(); // 遮罩在 OverlayLayer 最底层

            maskImage = maskObj.GetComponent<Image>();
            if (maskImage != null)
            {
                maskImage.raycastTarget = true;
                var maskBtn = maskObj.GetComponent<Button>();
                if (maskBtn == null) maskBtn = maskObj.AddComponent<Button>();
                maskBtn.onClick.RemoveAllListeners();
                maskBtn.onClick.AddListener(HideTopPanel);
                maskBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            }
        }

        // 预实例化所有面板并隐藏（按 PanelType 路由到对应层级）
        foreach (var entry in panelPrefabs)
        {
            if (entry.prefab != null)
            {
                Transform parent = GetLayer(entry.panelType);
                GameObject instance = Instantiate(entry.prefab, parent);
                instance.name = entry.panelName;
                instance.SetActive(false);
                panels[entry.panelName] = instance;
            }
        }

        // 实例化并初始化转场遮罩（放在 SystemLayer）
        if (Mask != null)
        {
            Mask = Instantiate(Mask, systemLayer);
            Mask.name = "Mask";
            Mask.SetActive(false);
            maskRadiusAnimator = Mask.GetComponent<MaskRadiusAnimator>();
        }
    }

    private void Start()
    {
    }

    /// <summary>
    /// 获取面板显示状态
    /// </summary>
    /// <param name="panelName">面板名称</param>
    public bool IsShown(string panelName)
    {
        return panels[panelName].activeSelf;
    }

    public void SetLoadingTip(string tip)
    {
        var loadingScreen = GetPanel("loading")?.GetComponent<LoadingScreen>();
        if (loadingScreen != null)
        {
            loadingScreen.SetTip(tip);
        }
    }
    public void ShowLoadingScreen(string tip)
    {
        Show("loading");
        SetLoadingTip(tip);
    }

    /// <summary>
    /// 显示指定名称的面板。若已显示则不重复创建。
    /// </summary>
    public void Show(string panelName, bool showMask = false, bool fadeIn = false,object data = null)
    {
        if (!panels.ContainsKey(panelName))
        {
            Logger.LogWarning($"UIManager: 面板 '{panelName}' 未注册。");
            return;
        }

        GameObject panel = panels[panelName];
        if (panel.activeSelf) return;

        panel.SetActive(true);

        if (fadeIn)
        {
            var images = panel.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                Color color = image.color;
                float originalAlpha = color.a;
                color.a = 0f;
                image.color = color;
                image.DOFade(originalAlpha, 0.3f);
            }
        }

        if (data != null) // 将数据传递给面板
        {
            var receiver = panel.GetComponent<IPanelDataReceiver>();
            if (receiver != null)
                receiver.OnReceiveData(data);
            else
                Logger.LogWarning($"UIManager: 面板 {panelName} 不支持数据传递。");
        }

        // 在所在层级容器中置顶（层间顺序由容器本身保证）
        panel.transform.SetAsLastSibling();
        panelStack.Push(panel);

        // 显示遮罩（与面板在同一 OverlayLayer，遮罩在下面）
        if (showMask && maskImage != null)
        {
            maskImage.gameObject.SetActive(true);
            maskImage.transform.SetAsLastSibling();
            panel.transform.SetAsLastSibling(); // 面板在遮罩之上
        }
    }

    /// <summary>
    /// 隐藏指定面板。
    /// </summary>
    public void Hide(string panelName, bool autoHideMask = true, bool fadeOut = false)
    {
        if (!panels.ContainsKey(panelName)) return;

        GameObject panel = panels[panelName];
        panel.SetActive(false);

        // 从栈中移除
        if (panelStack.Count > 0 && panelStack.Peek() == panel)
            panelStack.Pop();

        // 检查是否需要隐藏遮罩
        if (autoHideMask && maskImage != null && ShouldHideMask())
            HideMask();
    }

    /// <summary>
    /// 返回当前最上层面板的名称。
    /// </summary>
    public string GetTopPanelName()
    {
        return panelStack.Count > 0 ? panelStack.Peek().name : null;
    }

    /// <summary>
    /// 关闭最上层面板（常用于 ESC 返回）。
    /// </summary>
    public void HideTopPanel()
    {
        if (panelStack.Count > 0)
        {
            Hide(panelStack.Peek().name);
        }
    }

    /// <summary>
    /// 隐藏所有面板（用于场景切换）。
    /// </summary>
    public void HideAll()
    {
        foreach (var panel in panels.Values)
            if (panel != Mask)
            panel.SetActive(false);
        panelStack.Clear();
        HideMask();
    }

    /// <summary>
    /// 获取面板实例，以便进行细粒度操作。
    /// </summary>
    public GameObject GetPanel(string panelName)
    {
        return panels.ContainsKey(panelName) ? panels[panelName] : null;
    }

    public void ChangePanelsWithMask(string[] previousPanelNames, string[] nextPanelNames, float duration = 0.5f)
    {
        // 先设置MaskRadiusAnimator的动画持续时间
        if (maskRadiusAnimator != null)
        {
            maskRadiusAnimator.duration = duration;
            // 显示遮罩
            maskRadiusAnimator.gameObject.SetActive(true);
            // 调用MaskRadiusAnimator的PlayAnimation方法，播放前半段转场动画
            // Logger.Log("UIManager: 开始播放转场动画，等待动画结束后切换面板...");
            maskRadiusAnimator.PlayAnimation();
            // 停止旧的切换协程，防止重叠的 DelayedPanelSwitch 竞态
            if (panelSwitchCoroutine != null) StopCoroutine(panelSwitchCoroutine);
            panelSwitchCoroutine = StartCoroutine(DelayedPanelSwitch(previousPanelNames, nextPanelNames, duration));
        }
        else Logger.LogError("UIManager: MaskRadiusAnimator 组件未找到，无法执行遮罩转场动画。");
    }

    private System.Collections.IEnumerator DelayedPanelSwitch(string[] previousPanelNames, string[] nextPanelNames, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (previousPanelNames[0] == "all") // 特例：传入 "all" 则隐藏所有面板
        {
            HideAll();
        }
        else
        {
            // 隐藏之前的面板
            foreach (var panelName in previousPanelNames)
                Hide(panelName, false); // 不自动隐藏遮罩
        }

        // 显示新的面板
        foreach (var panelName in nextPanelNames)
            Show(panelName, false); // 不自动显示遮罩

        // 调用MaskRadiusAnimator的PlayAnimationReverse方法，播放后半段转场动画
        if (maskRadiusAnimator != null)
        {
            maskRadiusAnimator.PlayAnimationReverse();
            // 等待动画结束后隐藏遮罩
            yield return new WaitForSeconds(maskRadiusAnimator.duration + 0.01f);
            maskRadiusAnimator.gameObject.SetActive(false);
        }
        // 广播面板切换完成事件
        GameEventChannel.Dispatch(new PanelSwitchedEvent(previousPanelNames, nextPanelNames));
        panelSwitchCoroutine = null;
    }



    // ---------- 遮罩辅助方法 ----------
    private void HideMask()
    {
        if (maskImage != null)
            maskImage.gameObject.SetActive(false);
    }

    private bool ShouldHideMask()
    {
        // 只检查 OverlayLayer 中的面板是否有仍激活的
        foreach (var panel in panels.Values)
        {
            if (panel.transform.parent == overlayLayer && panel.activeSelf)
                return false;
        }
        return true;
    }

    // ---------- 层级容器辅助 ----------
    private Transform CreateLayer(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(mainCanvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.transform;
    }

    private Transform GetLayer(PanelType type)
    {
        switch (type)
        {
            case PanelType.HUD:     return backgroundLayer;
            case PanelType.Panel:   return panelLayer;
            case PanelType.Overlay: return overlayLayer;
            default:                return panelLayer;
        }
    }

}

/// <summary>
/// 用于在 Inspector 中注册面板预制体。
/// </summary>
[System.Serializable]
public class PanelEntry
{
    public string panelName;      // 面板标识符
    public GameObject prefab;     // 面板预制体
    public PanelType panelType = PanelType.Panel;  // 所属层级
}