using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    public void SetMasterVolume(float volume)
    {
        // todo: 实现设置主音量的逻辑
        Debug.Log($"设置主音量为 {volume}");
    }
    
    public void SetMusicVolume(float volume)
    {
        // todo: 实现设置音乐音量的逻辑
        Debug.Log($"设置音乐音量为 {volume}");
    }

    public void SetSFXVolume(float volume)
    {
        // todo: 实现设置音效音量的逻辑
        Debug.Log($"设置音效音量为 {volume}");
    }

}