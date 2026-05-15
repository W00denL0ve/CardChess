using System.Threading;
using UnityEngine;

/// <summary>
/// 游戏启动引导器，负责顺序初始化所有全局管理器，然后切换到主菜单。
/// </summary>
public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float simulatedLoadTime; // 模拟每步加载时延，release可移除

    public GameObject manager; // 管理器父物体

    private void Awake()
    {
        Logger.Log("Bootstrapper Awake：引导器已创建，创建管理器...");
        // 从Resource/Prefabs获取管理器物体并将管理器实例化，各个管理器挂在Manager的子物体上
        manager = Instantiate(Resources.Load<GameObject>("Prefabs/Manager"));
        // 不进行自动销毁
        DontDestroyOnLoad(manager);
        DontDestroyOnLoad(gameObject);
        // 订阅场景切换事件，备用
        GameEventChannel.Register<SceneChangedEvent>(OnSceneChanged);
        // 订阅面板切换事件，用于销毁引导器
        GameEventChannel.Register<PanelSwitchedEvent>(OnPanelChanged);
    }
    private async void Start()
    {
        await System.Threading.Tasks.Task.Yield(); // 等待一帧，确保所有Awake执行完毕

        // 执行初始化

        Initializer.Initialize();
        UIManager.Instance.ShowLoadingScreen("正在加载游戏资源"); // 显示加载界面

        await System.Threading.Tasks.Task.Delay((int)(simulatedLoadTime * 1000)); // 模拟加载时延

        // 应用用户设置（音量、画质等，在此之前存档管理器已初始化）
        ApplyUserSettings();

        await System.Threading.Tasks.Task.Delay((int)(simulatedLoadTime * 1000)); // 模拟加载时延

        // 加载主菜单场景
        SceneManager.Instance.LoadSceneAsync(mainMenuSceneName, () =>
        {
            // 加载标题面板UI
            UIManager.Instance.ChangePanelsWithMask(new string[] {"loading"},new string[] {"title"}, 1f);
            Logger.Log("主菜单场景加载完成");
        });
    }

    public void OnSceneChanged(SceneChangedEvent e)
    {
        Logger.Log($"Bootstrapper 收到场景切换事件：{e.PreviousScene} -> {e.CurrentScene}");
    }
    public void OnPanelChanged(PanelSwitchedEvent e)
    {
        Logger.Log($"Bootstrapper 收到面板切换事件，主菜单面板已加载，销毁引导器。");
        Destroy(gameObject); // 销毁引导器
    }
    private void OnDestroy()
    {
        // 取消订阅事件，清理资源
        GameEventChannel.Unregister<SceneChangedEvent>(OnSceneChanged);
        GameEventChannel.Unregister<PanelSwitchedEvent>(OnPanelChanged);
        Logger.Log("Bootstrapper 已销毁。");
    }

    private void ApplyUserSettings()
    {
        // 示例：从 SaveManager 读取设置并应用
        SettingsData settings = SaveManager.Instance?.LoadSettings();
        if (settings != null)
        {
            AudioManager.Instance.SetMasterVolume(settings.MasterVolume);
            AudioManager.Instance.SetMusicVolume(settings.MusicVolume);
            AudioManager.Instance.SetSFXVolume(settings.SfxVolume);
            QualitySettings.SetQualityLevel(settings.QualityLevel);
            Screen.SetResolution(settings.ScreenWidth, settings.ScreenHeight, settings.Fullscreen);
            Application.targetFrameRate = settings.TargetFrameRate;
        }
    }
}