using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public static class Initializer
{
    public static void Initialize()
    {
        // 初始化 PlayerPrefs 设置项
        PlayerPrefsInitializer.Initialize();
        
        // 其他全局初始化逻辑（如日志系统、错误处理等）可以放在这里
    }
}

/// <summary>
/// PlayerPrefs 初始化器，自动根据 SettingsData 的字段进行初始化。
/// </summary>
public static class PlayerPrefsInitializer
{
    /// <summary>
    /// 调用一次即可：对比当前 PlayerPrefs 与默认设置，补全缺失的键。
    /// </summary>
    public static void Initialize()
    {
        SettingsData defaults = new SettingsData();          // 默认值来源
        FieldInfo[] fields = typeof(SettingsData).GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            string key = field.Name;   // 键名直接使用字段名

            // 如果该键已经存在，跳过（保留玩家之前修改的值）
            if (PlayerPrefs.HasKey(key))
                continue;

            // 根据字段类型写入默认值
            if (field.FieldType == typeof(float))
            {
                float val = (float)field.GetValue(defaults);
                PlayerPrefs.SetFloat(key, val);
            }
            else if (field.FieldType == typeof(int))
            {
                int val = (int)field.GetValue(defaults);
                PlayerPrefs.SetInt(key, val);
            }
            else if (field.FieldType == typeof(bool))
            {
                bool val = (bool)field.GetValue(defaults);
                PlayerPrefs.SetInt(key, val ? 1 : 0);   // bool 以 int 形式存储
            }
            // 这里可以继续添加更多类型支持，如 string 等
        }

        PlayerPrefs.Save();
    }
}