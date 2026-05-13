using UnityEngine;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 公共类，用于PlayerPrefs的数据
/// </summary>
public class SettingsData
{
    public float MasterVolume = 0.8f;
    public float MusicVolume = 1.0f;
    public float SfxVolume = 1.0f;
    public int QualityLevel = 1;
    public int ScreenWidth = 1920;
    public int ScreenHeight = 1080;
    public bool Fullscreen = false;
    public int TargetFrameRate = 60;
    // 未来新增的设置项只需在此添加一行即可
}
public static class DemoPath
{
    public static string SaveFolder
    {
        get
        {
            // dataPath 末尾是 “_Data”，取它的上一级目录就是 exe 所在目录
            string exeDir = Path.GetDirectoryName(Application.dataPath);
            string saveDir = Path.Combine(exeDir, "Saves");
            // 确保目录存在
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);
            return saveDir;
        }
    }

    public static string GetSavePath(string fileName)
    {
        return Path.Combine(SaveFolder, fileName);
    }
}

[System.Serializable]
public class GameSaveData
{
    // 在这里添加存档字段
    // public int level;
    // public Vector3 playerPosition;  // 需要自行处理序列化
    // public List<string> inventory;
}

/// <summary>
/// 综合存档管理器：简单数据用 PlayerPrefs，复杂进度用 JSON 文件保存。
/// 演示版会将 JSON 文件放在游戏 exe 同级的 Saves 文件夹内。
/// </summary>
public class SaveManager : MonoBehaviour
{
    // 单例
    public static SaveManager Instance { get; private set; }

    // ──────────────────────────────────────
    // 2. 自定义存档路径（与之前演示版一致）
    // ──────────────────────────────────────
    private string SaveFolder
    {
        get
        {
            // 取 dataPath 的上一级目录（exe 所在目录）
            string exeDir = Path.GetDirectoryName(Application.dataPath);
            string folder = Path.Combine(exeDir, "Saves");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }
    }

    private string GetJsonSavePath(string fileName)
    {
        return Path.Combine(SaveFolder, fileName);
    }

    // ──────────────────────────────────────
    // 3. 初始化（保持单例）
    // ──────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ══════════════════════════════════════
    // PlayerPrefs 部分 —— 放少量简单数据
    // ══════════════════════════════════════

    /// <summary>保存整型到 PlayerPrefs</summary>
    public void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>读取整型</summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    /// <summary>保存浮点</summary>
    public void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>读取浮点</summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    /// <summary>保存字符串</summary>
    public void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    /// <summary>读取字符串</summary>
    public string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    /// <summary>保存布尔值（以 int 形式存储）</summary>
    public void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>读取布尔值</summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    /// <summary>删除某个 PlayerPrefs 键</summary>
    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    /// <summary>清空所有 PlayerPrefs</summary>
    public void DeleteAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    // ══════════════════════════════════════
    // JSON 文件部分 —— 保存复杂进度/存档
    // ══════════════════════════════════════

    /// <summary>保存游戏数据到 JSON 文件</summary>
    public void SaveToJson(GameSaveData data, string fileName = "save.json")
    {
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        string fullPath = GetJsonSavePath(fileName);
        File.WriteAllText(fullPath, json);
        Debug.Log($"[SaveManager] 存档已保存至：{fullPath}");
    }

    /// <summary>从 JSON 文件读取游戏数据</summary>
    public GameSaveData LoadFromJson(string fileName = "save.json")
    {
        string fullPath = GetJsonSavePath(fileName);
        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"[SaveManager] 存档读取成功：{fullPath}");
            return data;
        }
        Debug.LogWarning($"[SaveManager] 未找到存档文件：{fullPath}");
        return null;
    }

    /// <summary>删除指定 JSON 存档文件</summary>
    public void DeleteJsonFile(string fileName = "save.json")
    {
        string fullPath = GetJsonSavePath(fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Debug.Log($"[SaveManager] 已删除存档：{fullPath}");
        }
    }

    /// <summary>检查指定 JSON 存档是否存在</summary>
    public bool JsonFileExists(string fileName = "save.json")
    {
        return File.Exists(GetJsonSavePath(fileName));
    }

    /// <summary>
    /// 加载用户设置（如音量、画质等），在游戏启动时调用
     /// 从 PlayerPrefs 读取一些设置。
     /// 在 Bootstrapper 中调用 ApplyUserSettings() 来应用这些设置。
     /// 注意：确保在调用 ApplyUserSettings() 时，SaveManager 已经完成初始化。
    /// </summary>
    public SettingsData LoadSettings()
    {
        return new SettingsData
        {
            MasterVolume = GetFloat(nameof(SettingsData.MasterVolume), 0.8f),
            MusicVolume = GetFloat(nameof(SettingsData.MusicVolume), 1.0f),
            SfxVolume = GetFloat(nameof(SettingsData.SfxVolume), 1.0f),
            QualityLevel = GetInt(nameof(SettingsData.QualityLevel), 1),
            ScreenWidth = GetInt(nameof(SettingsData.ScreenWidth), 1920),
            ScreenHeight = GetInt(nameof(SettingsData.ScreenHeight), 1080),
            Fullscreen = GetBool(nameof(SettingsData.Fullscreen), false),
            TargetFrameRate = GetInt(nameof(SettingsData.TargetFrameRate), 60)
        };
    }
}