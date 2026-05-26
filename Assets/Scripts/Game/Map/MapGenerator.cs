using System;
using UnityEngine;

/// <summary>
/// 地图生成器，根据地图预设随机生成关卡以及路线
/// </summary>
public class MapGenerator : MonoBehaviour
{
    public static MapGenerator Instance { get; private set; }

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
    /// 根据给定的随机种子生成地图，触发地图生成事件
    /// </summary>
    /// <param name="seed"></param>
    public void GenerateMap(int seed)
    {
        //TODO
    }
}

