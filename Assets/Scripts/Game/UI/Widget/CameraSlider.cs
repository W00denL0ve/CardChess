using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 相机速度滑动条：参照 VolumeSlider 模式，主动从 CameraController 拉取值，修改时调用对应 Set 方法。
/// 存储逻辑由 CameraController 负责。
/// </summary>
public class CameraSlider : MonoBehaviour
{
    /// <summary>控制类型</summary>
    public enum CameraSettingType
    {
        KeyboardPan,
        DragPan,
        Zoom
    }

    [Tooltip("选择此滑动条控制的相机速度类型")]
    public CameraSettingType type = CameraSettingType.KeyboardPan;

    private Slider slider;
    private bool isSettingValueInternally;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (slider == null)
        {
            Logger.LogError($"[CameraSlider] 物体 {gameObject.name} 缺少 Slider 组件，已禁用脚本。");
            enabled = false;
            return;
        }

        // 整数步进：1-5
        slider.wholeNumbers = true;
        slider.minValue = 1;
        slider.maxValue = 5;
    }

    private void OnEnable()
    {
        RefreshValueFromCameraController();

        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    /// <summary>从 CameraController 读取当前值并设置到 Slider</summary>
    private void RefreshValueFromCameraController()
    {
        if (CameraController.Instance == null)
        {
            Logger.Log("[CameraSlider] CameraController 未就绪，无法刷新值。");
            return;
        }

        isSettingValueInternally = true;
        switch (type)
        {
            case CameraSettingType.KeyboardPan:
                slider.value = CameraController.Instance.KeyboardPanSpeed;
                break;
            case CameraSettingType.DragPan:
                slider.value = CameraController.Instance.DragPanSpeed;
                break;
            case CameraSettingType.Zoom:
                slider.value = CameraController.Instance.ZoomSpeed;
                break;
        }
        isSettingValueInternally = false;
    }

    /// <summary>Slider 值改变时的回调，将新值设置给 CameraController</summary>
    private void OnSliderValueChanged(float newValue)
    {
        if (isSettingValueInternally) return;

        if (CameraController.Instance == null)
        {
            Logger.LogWarning("[CameraSlider] CameraController 未就绪，无法修改值。");
            return;
        }

        int intValue = Mathf.RoundToInt(newValue);
        switch (type)
        {
            case CameraSettingType.KeyboardPan:
                CameraController.Instance.SetKeyboardPanSpeed(intValue);
                break;
            case CameraSettingType.DragPan:
                CameraController.Instance.SetDragPanSpeed(intValue);
                break;
            case CameraSettingType.Zoom:
                CameraController.Instance.SetZoomSpeed(intValue);
                break;
        }
    }
}
