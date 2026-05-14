using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局 UI 管理器，负责面板的显示、隐藏、层级和遮罩。
/// 挂载在 Boot 场景的 Managers 子物体上。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("主画布")]
    [SerializeField] private Canvas mainCanvas;

    [Header("面板预制体注册")]
    [SerializeField] private List<PanelEntry> panelPrefabs = new List<PanelEntry>();

    [Header("背景遮罩")]
    [SerializeField] private Image backgroundMask; // 半透明背景遮罩，用于弹窗
    [SerializeField] private float maskAlpha = 0.5f;

    [Header("转场遮罩")]
    [SerializeField] private GameObject Mask; // 初始化时实例化并隐藏
    
    private MaskRadiusAnimator maskRadiusAnimator; // 转场用的镂空遮罩动画组件

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

        // 确保遮罩初始隐藏
        if (backgroundMask != null)
            backgroundMask.gameObject.SetActive(false);

        // 预实例化所有面板并隐藏
        foreach (var entry in panelPrefabs)
        {
            if (entry.prefab != null)
            {
                GameObject instance = Instantiate(entry.prefab, mainCanvas.transform);
                instance.name = entry.panelName;
                instance.SetActive(false);
                panels[entry.panelName] = instance;
            }
        }

        // 实例化并初始化转场遮罩
        if (Mask != null)
        {
            Mask = Instantiate(Mask, mainCanvas.transform);
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
    public void Show(string panelName, bool showMask = false)
    {
        if (!panels.ContainsKey(panelName))
        {
            Debug.LogWarning($"UIManager: 面板 '{panelName}' 未注册。");
            return;
        }

        GameObject panel = panels[panelName];
        if (panel.activeSelf) return;

        panel.SetActive(true);
        if (panelName != "HUD") // HUD保持在最底层
        {
            panel.transform.SetSiblingIndex(Mask.transform.GetSiblingIndex() - 1); // 放在转场遮罩下一层
        }
        panelStack.Push(panel);

        if (showMask && backgroundMask != null)
        {
            backgroundMask.gameObject.SetActive(true);
            SetMaskAlpha(maskAlpha);
            // 遮罩放在面板下层，其余内容上层
            backgroundMask.transform.SetSiblingIndex(panel.transform.GetSiblingIndex() - 1);
        }
    }

    /// <summary>
    /// 隐藏指定面板。
    /// </summary>
    public void Hide(string panelName, bool autoHideMask = true)
    {
        if (!panels.ContainsKey(panelName)) return;

        GameObject panel = panels[panelName];
        panel.SetActive(false);

        // 从栈中移除
        if (panelStack.Count > 0 && panelStack.Peek() == panel)
            panelStack.Pop();

        // 检查是否需要隐藏遮罩
        if (autoHideMask && backgroundMask != null && ShouldHideMask())
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
            Debug.Log("UIManager: 开始播放转场动画，等待动画结束后切换面板...");
            maskRadiusAnimator.PlayAnimation();
            // duration秒后执行切换面板的逻辑，采用协程实现（便于传递参数）
            StartCoroutine(DelayedPanelSwitch(previousPanelNames, nextPanelNames, duration));
        }
        else Debug.LogError("UIManager: MaskRadiusAnimator 组件未找到，无法执行遮罩转场动画。");
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
            yield return new WaitForSeconds(maskRadiusAnimator.duration);
            maskRadiusAnimator.gameObject.SetActive(false);
        }
        // 广播面板切换完成事件
        GameEventChannel.Dispatch(new PanelSwitchedEvent(previousPanelNames, nextPanelNames));
    }



    // ---------- 遮罩辅助方法 ----------
    private void HideMask()
    {
        if (backgroundMask != null)
            backgroundMask.gameObject.SetActive(false);
    }

    private bool ShouldHideMask()
    {
        foreach (var panel in panels.Values)
        {
            if (panel.activeSelf) return false;
        }
        return true;
    }

    private void SetMaskAlpha(float alpha)
    {
        if (backgroundMask != null)
        {
            Color c = backgroundMask.color;
            c.a = alpha;
            backgroundMask.color = c;
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
}