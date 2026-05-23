using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音量滑动条：主动从 AudioManager 拉取值，修改时调用 AudioManager 对应方法。
/// 存储逻辑由 AudioManager 负责。
/// </summary>
public class VolumeSlider : MonoBehaviour
{
    /// <summary>控制类型：音乐、音效、主音量</summary>
    public enum VolumeType
    {
        Music,
        Sound,
        Master
    }

    [Tooltip("选择此滑动条控制的音量类型")]
    public VolumeType type = VolumeType.Music;

    private Slider slider;
    private bool isSettingValueInternally; // 防止因代码设置 value 而触发 OnValueChanged

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Debug.LogError($"[VolumeSlider] 物体 {gameObject.name} 缺少 Slider 组件，已禁用脚本。", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // 每次启用时（例如打开设置面板）从 AudioManager 拉取当前音量，保证显示最新值
        RefreshValueFromAudioManager();

        // 监听滑动条值变化
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    /// <summary>从 AudioManager 读取当前音量并设置到 Slider</summary>
    private void RefreshValueFromAudioManager()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSlider] AudioManager 未就绪，无法刷新音量值。");
            return;
        }

        isSettingValueInternally = true;
        switch (type)
        {
            case VolumeType.Music:
                slider.value = AudioManager.Instance.MusicVolume;
                break;
            case VolumeType.Sound:
                slider.value = AudioManager.Instance.SoundVolume;
                break;
            case VolumeType.Master:
                slider.value = AudioManager.Instance.MasterVolume;
                break;
        }
        isSettingValueInternally = false;
    }

    /// <summary>Slider 值改变时的回调，将新值设置给 AudioManager</summary>
    private void OnSliderValueChanged(float newValue)
    {
        // 避免因 RefreshValueFromAudioManager 设置 value 时触发本方法
        if (isSettingValueInternally) return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[VolumeSlider] AudioManager 未就绪，无法修改音量。");
            return;
        }

        switch (type)
        {
            case VolumeType.Music:
                AudioManager.Instance.SetMusicVolume(newValue);
                break;
            case VolumeType.Sound:
                AudioManager.Instance.SetSoundVolume(newValue);
                break;
            case VolumeType.Master:
                AudioManager.Instance.SetMasterVolume(newValue);
                break;
        }
    }
}