using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("音量 UI（可选）")]
    [Tooltip("可为空，没有 Slider 时使用默认值")]
    public Slider musicVolumeSlider;
    public Slider soundVolumeSlider;

    // 音频源
    private AudioSource musicSource;
    private List<AudioSource> soundSources = new();
    private List<AudioSource> loopSoundSources = new();

    // 运行时缓存
    private float masterVolume = 1f;
    private float cachedMusicVolume = 0.5f;
    private float cachedSoundVolume = 0.5f;
    private Dictionary<string, AudioClip> audioCache = new();

    private const float DefaultVolume = 0.5f;
    private const string MusicVolumeKey = "MusicVolume";
    private const string SoundVolumeKey = "SoundVolume";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        soundSourceCount = Mathf.Clamp(soundSourceCount, 1, 16);
        loopSoundSourceCount = Mathf.Clamp(loopSoundSourceCount, 1, 8);

        // 音频源
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        for (int i = 0; i < soundSourceCount; i++)
            soundSources.Add(CreateSource(false));
        for (int i = 0; i < loopSoundSourceCount; i++)
            loopSoundSources.Add(CreateSource(true));

        // 音量初始化（从 SaveManager 读，首次设默认值）
        var save = SaveManager.Instance;
        if (save != null && !save.GetBool("HasLaunched"))
        {
            save.SetFloat(MusicVolumeKey, DefaultVolume);
            save.SetFloat(SoundVolumeKey, DefaultVolume);
            save.SetBool("HasLaunched", true);
        }
        float master = save?.GetFloat("MasterVolume", 1f) ?? 1f;
        float musicVol = save?.GetFloat(MusicVolumeKey, DefaultVolume) ?? DefaultVolume;
        float soundVol = save?.GetFloat(SoundVolumeKey, DefaultVolume) ?? DefaultVolume;

        SetMasterVolume(master);
        SetMusicVolume(musicVol);
        SetSoundVolume(soundVol);

        // 绑定 UI Slider（可选）
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVol;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.value = soundVol;
            soundVolumeSlider.onValueChanged.AddListener(OnSoundSliderChanged);
        }
    }

    private AudioSource CreateSource(bool loop)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }

    void OnDestroy()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        if (soundVolumeSlider != null)
            soundVolumeSlider.onValueChanged.RemoveListener(OnSoundSliderChanged);
    }

    // ====================================================================
    //  音量
    // ====================================================================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume();
        ApplySoundVolume();
    }

    public void SetMusicVolume(float volume)
    {
        cachedMusicVolume = Mathf.Clamp01(volume);
        SaveManager.Instance?.SetFloat(MusicVolumeKey, volume);
        ApplyMusicVolume();
    }

    public void SetSoundVolume(float volume)
    {
        cachedSoundVolume = Mathf.Clamp01(volume);
        SaveManager.Instance?.SetFloat(SoundVolumeKey, volume);
        ApplySoundVolume();
    }

    private void ApplyMusicVolume()
    {
        musicSource.volume = cachedMusicVolume * masterVolume;
    }

    private void ApplySoundVolume()
    {
        float v = cachedSoundVolume * masterVolume;
        foreach (var s in soundSources) s.volume = v;
        foreach (var s in loopSoundSources) s.volume = v;
    }

    private void OnMusicSliderChanged(float v) => SetMusicVolume(v);
    private void OnSoundSliderChanged(float v) => SetSoundVolume(v);

    public float MusicVolume => cachedMusicVolume;
    public float SoundVolume => cachedSoundVolume;
    public float MasterVolume => masterVolume;

    // ====================================================================
    //  音乐
    // ====================================================================

    public void PlayMusic(string musicName, bool fade = false)
    {
        if (string.IsNullOrEmpty(musicName)) return;

        if (fade && musicSource.isPlaying)
        {
            float targetVolume = cachedMusicVolume * masterVolume;
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
    //  音效
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
        return 0; // 全在忙则覆盖第一个
    }

    // ====================================================================
    //  循环音效获取
    // ====================================================================

    public AudioClip GetLoopSoundClip(int index = 0)
    {
        if (index >= 0 && index < loopSoundSources.Count)
            return loopSoundSources[index].clip;
        return null;
    }
}

