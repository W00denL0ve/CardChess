using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 负责显示/更新加载界面。
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI tipText;

    private void OnEnable()
    {
        // 每次激活时，确保自己在 Canvas 的最上层
        transform.SetAsLastSibling();
    }
    private void Start()
    {
        // 可根据喜好决定是否在游戏一开始就显示
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 设置进度条值 (0~1)。
    /// </summary>
    public void SetProgress(float progress)
    {
        if (progressBar != null)
            progressBar.value = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// 设置提示文字，比如 "正在初始化音频管理器..."
    /// </summary>
    public void SetTip(string message)
    {
        if (tipText != null)
            tipText.text = message;
    }

    /// <summary>
    /// 加载完成后隐藏界面（也可以在这里触发淡出动画）。
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}