using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

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

    /// <summary>
    /// 同步加载场景，直接切换到指定场景（不建议使用）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 异步加载场景，先等待 1 秒，再开始加载，加载完成后执行回调
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <param name="onComplete">加载完成后的回调</param>
    public void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName, onComplete));
    }

    /// <summary>
    /// 异步加载场景的协程：先固定等待 1 秒，再开始加载
    /// </summary>
    private IEnumerator LoadSceneAsyncCoroutine(string sceneName, Action onComplete)
    {
        // 1. 先等待 1 秒（不受 Time.timeScale 影响）
        yield return new WaitForSecondsRealtime(1f);

        // 2. 开始异步加载场景
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        // 3. 等待加载完成
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 4. 加载完成后立即执行回调
        onComplete?.Invoke();
    }

    public void ReloadCurrentScene()
    {
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.name);
    }
}