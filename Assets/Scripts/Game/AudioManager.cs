using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频路径")]
    public string musicPath = "Audio/Music";
    public string soundPath = "Audio/Sound";

    [Header("音频源数量")]
    public int soundSourceCount = 4;
    public int loopSoundSourceCount = 2;

    // 音频源
    private AudioSource musicSource;
    private List<AudioSource> soundSources = new();
    private List<AudioSource> loopSoundSources = new();

    // 运行时缓存
    private float masterVolume = 1f;
    private float musicVolume = 0.5f;
    private float soundVolume = 0.5f;
    private Dictionary<string, AudioClip> audioCache = new();

    // PlayerPrefs 键名（与 SaveManager.LoadSettings 保持一致）
    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SfxVolume";     // 统一为 SfxVolume

    private const float DefaultMasterVolume = 1f;
    private const float DefaultMusicVolume = 0.5f;
    private const float DefaultSoundVolume = 0.5f;
    private const string HasLaunchedKey = "HasLaunched";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始化音频源数量限制
        soundSourceCount = Mathf.Clamp(soundSourceCount, 1, 16);
        loopSoundSourceCount = Mathf.Clamp(loopSoundSourceCount, 1, 8);

        // 创建音频源
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        for (int i = 0; i < soundSourceCount; i++)
            soundSources.Add(CreateSource(false));
        for (int i = 0; i < loopSoundSourceCount; i++)
            loopSoundSources.Add(CreateSource(true));

        // 首次启动写入默认值
        var save = SaveManager.Instance;
        if (save != null && !save.GetBool(HasLaunchedKey))
        {
            save.SetFloat(MasterVolumeKey, DefaultMasterVolume);
            save.SetFloat(MusicVolumeKey, DefaultMusicVolume);
            save.SetFloat(SoundVolumeKey, DefaultSoundVolume);
            save.SetBool(HasLaunchedKey, true);
        }

        // 从持久层读取音量值
        masterVolume = save?.GetFloat(MasterVolumeKey, DefaultMasterVolume) ?? DefaultMasterVolume;
        musicVolume  = save?.GetFloat(MusicVolumeKey, DefaultMusicVolume) ?? DefaultMusicVolume;
        soundVolume  = save?.GetFloat(SoundVolumeKey, DefaultSoundVolume) ?? DefaultSoundVolume;

        // 应用到音频源
        ApplyMusicVolume();
        ApplySoundVolume();
    }

    private AudioSource CreateSource(bool loop)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }

    // ====================================================================
    //  音量控制（公开方法，供 UI 或其它系统调用）
    // ====================================================================

    /// <summary>设置主音量（会同时影响音乐和音效的实际输出）</summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveManager.Instance?.SetFloat(MasterVolumeKey, masterVolume);
        ApplyMusicVolume();
        ApplySoundVolume();
    }

    /// <summary>设置音乐音量（原始值，最终输出 = musicVolume * masterVolume）</summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        SaveManager.Instance?.SetFloat(MusicVolumeKey, musicVolume);
        ApplyMusicVolume();
    }

    /// <summary>设置音效音量（原始值，最终输出 = soundVolume * masterVolume）</summary>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        SaveManager.Instance?.SetFloat(SoundVolumeKey, soundVolume);
        ApplySoundVolume();
    }

    private void ApplyMusicVolume()
    {
        musicSource.volume = musicVolume * masterVolume;
    }

    private void ApplySoundVolume()
    {
        float finalVol = soundVolume * masterVolume;
        foreach (var s in soundSources) s.volume = finalVol;
        foreach (var s in loopSoundSources) s.volume = finalVol;
    }

    // 公开属性，供 UI 主动拉取当前值
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SoundVolume => soundVolume;

    // ====================================================================
    //  音乐播放
    // ====================================================================

    public void PlayMusic(string musicName, bool fade = false)
    {
        if (string.IsNullOrEmpty(musicName)) return;

        if (fade && musicSource.isPlaying)
        {
            float targetVolume = musicVolume * masterVolume;
            musicSource.DOFade(0f, 0.5f).OnComplete(() =>
            {
                musicSource.clip = LoadClip($"{musicPath}/{musicName}");
                if (musicSource.clip != null)
                {
                    musicSource.Play();
                    musicSource.DOFade(targetVolume, 0.5f);
                }
            });
        }
        else
        {
            musicSource.clip = LoadClip($"{musicPath}/{musicName}");
            if (musicSource.clip != null) musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    public AudioClip GetCurrentMusicClip() => musicSource.clip;

    // ====================================================================
    //  音效播放
    // ====================================================================

    public void PlaySound(string soundName)
    {
        PlaySoundOnSource(FindFreeSoundSource(), soundName, false);
    }

    public void PlayLoopSound(string soundName)
    {
        PlaySoundOnSource(0, soundName, true);
    }

    public void PlaySound(int index, string soundName)
    {
        PlaySoundOnSource(index, soundName, false);
    }

    public void PlayLoopSound(int index, string soundName)
    {
        PlaySoundOnSource(index, soundName, true);
    }

    public void StopLoopSound(int index = 0)
    {
        if (index >= 0 && index < loopSoundSources.Count)
        {
            loopSoundSources[index].Stop();
            loopSoundSources[index].clip = null;
        }
    }

    private void PlaySoundOnSource(int index, string soundName, bool isLoop)
    {
        var sources = isLoop ? loopSoundSources : soundSources;
        if (index < 0 || index >= sources.Count) return;

        if (string.IsNullOrEmpty(soundName))
        {
            sources[index].Stop();
            sources[index].clip = null;
            return;
        }

        sources[index].clip = LoadClip($"{soundPath}/{soundName}");
        sources[index].Play();
    }

    private AudioClip LoadClip(string path)
    {
        if (!audioCache.TryGetValue(path, out var clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>(path);
            if (clip != null) audioCache[path] = clip;
        }
        return clip;
    }

    private int FindFreeSoundSource()
    {
        for (int i = 0; i < soundSources.Count; i++)
            if (!soundSources[i].isPlaying)
                return i;
        return 0;
    }

    public AudioClip GetLoopSoundClip(int index = 0)
    {
        if (index >= 0 && index < loopSoundSources.Count)
            return loopSoundSources[index].clip;
        return null;
    }
}