using System.Collections;
using UnityEngine;
using Cinemachine;
using System;

public class CameraEffectManager : MonoBehaviour
{
    public static CameraEffectManager Instance { get; private set; }

    [Header("虚拟相机预制体(Awake创建)")]
    [Tooltip("特写聚焦相机（平时优先级为0，特效时临时提升）")]
    public GameObject virtualCamera;

    private CinemachineVirtualCamera focusVCam;
    private CinemachineImpulseSource impulseSource; // 固定在虚拟相机上的震动源

    [Header("效果参数")]
    [Tooltip("聚焦时虚拟相机的优先级（高于主相机的默认优先级，但因为没有常规虚拟相机，所以只要 >0 即可）")]
    public int focusPriority = 100;
    [Tooltip("聚焦持续时间结束后是否自动恢复主相机控制（如果不恢复，相机将停留在聚焦结束时的位置）")]
    public bool resetToOriginalPositionAfterFocus = false; // 根据需求决定

    private MonoBehaviour cameraControllerScript; // 主相机移动脚本组件，特效时禁用

    private Coroutine currentEffectRoutine;
    private int originalFocusPriority;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

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
        virtualCamera = Instantiate(virtualCamera);
        DontDestroyOnLoad(virtualCamera);
        focusVCam = virtualCamera.GetComponent<CinemachineVirtualCamera>();
        impulseSource = virtualCamera.GetComponent<CinemachineImpulseSource>();
        cameraControllerScript = gameObject.GetComponent<CameraController>();
    }

    private void Start()
    {
        if (focusVCam == null)
        {
            Debug.LogError("CameraEffectManager: 未分配 focusVCam！");
            return;
        }
        // 保存初始优先级，并确保其为0（或很低）
        originalFocusPriority = focusVCam.Priority;
        focusVCam.Priority = 0;
        // 确保跟随目标为空
        focusVCam.Follow = null;
        focusVCam.LookAt = null;

        SetVCamActive(false);
    }

    private void OnDisable()
    {
        if (currentEffectRoutine != null)
            StopCoroutine(currentEffectRoutine);
        StopFocus();
        Time.timeScale = 1f;
    }

    #region 公开方法

    public void SetVCamActive(bool active = true)
    {
        focusVCam.gameObject.SetActive(active);
    }

    /// <summary>
    /// 慢动作效果（不依赖虚拟相机）
    /// </summary>
    public void SlowMotion(float duration, float timeScale = 0.2f)
    {
        if (currentEffectRoutine != null)
            StopCoroutine(currentEffectRoutine);
        currentEffectRoutine = StartCoroutine(SlowMotionRoutine(duration, timeScale));
    }

    /// <summary>
    /// 镜头震动（立即，不依赖虚拟相机，但需要虚拟相机处于激活状态才有意义）
    /// 建议在聚焦时调用震动，或单独为你的移动相机添加 Impulse Listener
    /// </summary>
    public void ShakeCamera(float force = -1f)
    {
        if (impulseSource == null) return;
        if (force < 0)
            impulseSource.GenerateImpulse();
        else
            impulseSource.GenerateImpulse(force);
    }

    /// <summary>
    /// 聚焦到指定目标（临时启用虚拟相机）
    /// </summary>
    /// <param name="target">目标Transform</param>
    /// <param name="duration">聚焦持续时间（0表示无限，需手动调用StopFocus）</param>
    /// <param name="offset">相机偏移（相对目标）</param>
    public void FocusOnTarget(Transform target, float duration = 2f, Vector3? offset = null)
    {
        if (focusVCam == null) return;

        // 禁用主相机移动脚本
        if (cameraControllerScript != null)
            cameraControllerScript.enabled = false;

        // 停止当前特效
        if (currentEffectRoutine != null)
            StopCoroutine(currentEffectRoutine);

        // 记录当前主相机状态（如果需要复位）
        originalCameraPosition = Camera.main.transform.position;
        originalCameraRotation = Camera.main.transform.rotation;

        // 设置聚焦相机的跟随目标
        focusVCam.Follow = target;
        focusVCam.LookAt = target;

        // 可选：动态修改偏移
        if (offset.HasValue)
        {
            var transposer = focusVCam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
                transposer.m_FollowOffset = offset.Value;
        }

        // 提升优先级，激活虚拟相机（Brain 会自动接管主相机）
        focusVCam.Priority = focusPriority;

        if (duration > 0)
        {
            currentEffectRoutine = StartCoroutine(AutoStopFocusRoutine(duration));
        }
        else
        {
            currentEffectRoutine = null;
        }
    }

    /// <summary>
    /// 停止聚焦，恢复常规相机控制
    /// </summary>
    public void StopFocus()
    {
        if (cameraControllerScript != null)
            cameraControllerScript.enabled = true;

        if (currentEffectRoutine != null)
        {
            StopCoroutine(currentEffectRoutine);
            currentEffectRoutine = null;
        }

        if (focusVCam != null)
        {
            // 降低优先级，使虚拟相机失效
            focusVCam.Priority = 0;
            // 可选：清除跟随目标
            focusVCam.Follow = null;
            focusVCam.LookAt = null;
        }

        // 如果需要在结束后复位相机位置（根据你的需求决定是否启用）
        if (resetToOriginalPositionAfterFocus)
        {
            Camera.main.transform.position = originalCameraPosition;
            Camera.main.transform.rotation = originalCameraRotation;
        }

        SetVCamActive(false); // 自动取消激活
    }

    /// <summary>
    /// 组合效果（胜利特效）
    /// </summary>
    public void PlayCombinedEffect(Transform focusTarget, float slowMoDuration = 2f, float slowMoScale = 0.2f,
                                   float shakeDelay = 0.7f, float shakeForce = -1f, float focusDuration = 3f)
    {
        if (currentEffectRoutine != null)
            StopCoroutine(currentEffectRoutine);
        currentEffectRoutine = StartCoroutine(CombinedEffectRoutine(focusTarget, slowMoDuration, slowMoScale,
                                                                     shakeDelay, shakeForce, focusDuration));
    }

    #endregion

    #region 内部协程

    private IEnumerator SlowMotionRoutine(float duration, float timeScale)
    {
        float original = Time.timeScale;
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = original;
        currentEffectRoutine = null;
    }

    private IEnumerator AutoStopFocusRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        StopFocus();
        currentEffectRoutine = null;
    }

    private IEnumerator CombinedEffectRoutine(Transform target, float slowMoDuration, float timeScale,
                                              float shakeDelay, float shakeForce, float focusDuration, Action OnComplete = null)
    {
        // 开始聚焦
        FocusOnTarget(target, duration: 0); // 无限聚焦，我们自己控制停止时机
        // 慢动作
        float originalTimeScale = Time.timeScale;
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(slowMoDuration);
        Time.timeScale = originalTimeScale;
        // 延迟震动
        yield return new WaitForSecondsRealtime(shakeDelay);
        ShakeCamera(shakeForce);
        // 继续等待剩余的聚焦时间
        float remaining = focusDuration - slowMoDuration - shakeDelay;
        if (remaining > 0)
            yield return new WaitForSecondsRealtime(remaining);
        // 停止聚焦，恢复常规相机
        StopFocus();
        currentEffectRoutine = null;
    }

    #endregion
}