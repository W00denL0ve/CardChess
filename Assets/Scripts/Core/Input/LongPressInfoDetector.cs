using UnityEngine;

/// <summary>
/// 长按信息检测器
/// 监听长按完成事件，根据目标类型显示对应的信息面板
/// </summary>
public class LongPressInfoDetector : MonoBehaviour
{
    [Header("面板名称配置")]
    [SerializeField] private string unitInfoPanelName = "UnitInfoPanel";
    [SerializeField] private bool showMask = true;
    [SerializeField] private bool fadeIn = true;

    private void OnEnable()
    {
        GameEventChannel.Register<LongPressPerformedEvent>(OnLongPressPerformed);
    }

    private void OnDisable()
    {
        GameEventChannel.Unregister<LongPressPerformedEvent>(OnLongPressPerformed);
    }

    private void OnLongPressPerformed(LongPressPerformedEvent evt)
    {
        // 根据目标类型显示不同面板
        if (evt.Target is Unit unit)
        {
            ShowInfoPanel(unitInfoPanelName, unit);
        }
        // 未来可拓展其他类型:
        // else if (evt.Target is Cell cell) { ... }
        // else if (evt.Target is Card card) { ... }
    }

    private void ShowInfoPanel(string panelName, object data)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Show(panelName, showMask, fadeIn, data);
        }
        else
        {
            Logger.LogWarning("LongPressInfoDetector: UIManager 实例不存在，无法显示面板。");
        }
    }
}
