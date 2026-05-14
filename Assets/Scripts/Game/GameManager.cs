using System.Collections.Generic;
using System.Collections;
using System.Diagnostics.SymbolStore;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using System;

/// <summary>
/// 游戏管理器，负责整体游戏流程控制、全局状态管理等
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public Player player;
    public List<Character> allCharacters = new List<Character>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
    }

    private void Start()
    {
        // TurnManager.Instance.StartTurn();
    }

    private void OnDestroy()
    {
        Debug.Log("GameManager 已销毁，其余管理器应该也已销毁");
    }

    public void OnCardPlayed(CardData card)
    {
        if (!DeckManager.Instance.IsCardInHand(card)) return;

        DeckManager.Instance.PlayCard(card);
        EffectManager.Instance.ExecuteCardEffects(card);
    }

    /// <summary>
    /// 玩家在主菜单点击开始游戏后调用，触发游戏开始事件
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("GameManager：开始新游戏，触发游戏开始事件...");
        UIManager.Instance.SetLoadingTip("正在加载游戏地图...");
        UIManager.Instance.ChangePanelsWithMask(new string[] { "all" }, new string[] { "loading" });
        // 异步切换到地图场景，加载完成后生成地图并触发游戏开始事件
        SceneManager.Instance.LoadSceneAsync("Map", () =>
        {
            Debug.Log("地图场景加载完成，生成地图...");
            MapGenerator.Instance.GenerateMap(seed: UnityEngine.Random.Range(0, int.MaxValue));
            Debug.Log("地图生成完成，触发游戏开始事件...");
            UIManager.Instance.ChangePanelsWithMask(new string[] { "loading" }, new string[] { "map", "HUD" });
            GameEventChannel.Dispatch(new MapEnteredEvent());
        }); // todo:地图生成器采用异步逻辑，UI更新放在第二个回调中
    }

    public void ContinueGame()
    {
        // todo:从存档继续游戏
    }

    /// <summary>
    /// 玩家在地图界面点击进入关卡后调用，触发进入关卡事件
    /// </summary>
    /// <param name="levelName">关卡场景名</param>
    public void EnterLevel(string levelName)
    {
        LoadLevelAsync(levelName);
    }

    public void GamePause()
    {
        Time.timeScale = 0f;
    }

    public void GameResume()
    {
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        // todo: 实现游戏结束逻辑
    }

    public void BackToMainMenu()
    {
        //todo UIManager弹出提示框
        GameResume(); // 确保游戏时间恢复正常
        UIManager.Instance.SetLoadingTip("正在返回主菜单...");
        UIManager.Instance.ChangePanelsWithMask(new string[] { "all" }, new string[] { "loading" });
        // 异步切换场景
        SceneManager.Instance.LoadSceneAsync("MainMenu", () =>
        {
            Debug.Log("主菜单场景加载完成");
            // 切换到标题面板
            UIManager.Instance.ChangePanelsWithMask(new string[] { "loading" }, new string[] { "title" });
        });
    }
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    /// <summary>
    /// 异步加载关卡重载方法，按照命名规范，关卡数据名称为场景名称
    /// </summary>
    /// <param name="sceneName"></param>
    public void LoadLevelAsync(string sceneName)
    {
        LoadLevelAsync(sceneName, sceneName);
    }

    /// <summary>
    /// 根据 Addressable 键加载关卡
    /// </summary>
    /// <param name="levelDataAddress">LevelData 的 Addressable Key</param>
    /// <param name="sceneName">关卡场景名</param>
    public void LoadLevelAsync(string sceneName, string levelDataAddress)
    {
        Debug.Log("正在加载场景...");
        UIManager.Instance.SetLoadingTip("正在加载场景...");
        UIManager.Instance.ChangePanelsWithMask(new string[] {"all"}, new string[] {"loading"});
        // 1. 异步加载关卡场景
        SceneManager.Instance.LoadSceneAsync(sceneName, () => 
        {
            Debug.Log("场景加载完成，正在加载关卡数据...");
            UIManager.Instance.SetLoadingTip("正在加载关卡数据...");
            // 实例化关内管理器
            Instantiate(Resources.Load("Prefabs/ManagersInLevel"));
            StartCoroutine(LoadLevelCoroutine(levelDataAddress, () =>
            {
                Debug.Log("关卡数据加载完成");
                GameEventChannel.Dispatch(new LevelEnteredEvent(sceneName));
                UIManager.Instance.ChangePanelsWithMask(new string[] { "loading" }, new string[] {"HUD"});
            }));
        });
    }

    IEnumerator LoadLevelCoroutine(string levelDataAddress, Action onComplete)
    {
        // 1. 异步加载 LevelData
        var loadHandle = Addressables.LoadAssetAsync<LevelData>(levelDataAddress);
        while (!loadHandle.IsDone)
        {
            yield return null;   
        }
        
        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            LevelData levelData = loadHandle.Result;

            // 3. 场景加载完成，查找 LevelManager 并传入数据
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
                levelManager.Initialize(levelData);
            else
                Debug.LogError("场景中未找到 LevelManager！");

            // 注意：不要在这里释放 LevelData 的 Handle，它由 LevelManager 控制生命周期

            onComplete?.Invoke();
        }
        else
        {
            Debug.LogError($"加载 LevelData 失败: {loadHandle.OperationException}");
        }
    }

}