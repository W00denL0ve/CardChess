using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡内相机控制 — 透视相机，固定俯角，沿视线方向缩放
///
/// 设计模型：
///   相机有一个固定的观察目标点（在网格平面上），相机位于目标点距 d 的反向视线方向上。
///   平移 → 移动目标点（受网格边界约束）
///   缩放 → 改变距离 d（Y 和 Z 按俯角比例同时变化）
///
/// 操作：
///   方向键/鼠标右键拖动 → 平移
///   滚轮                → 缩放
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("相机姿态")]
    public float xRotation = 45f;

    [Header("平移")]
    [SerializeField] private int keyboardPanSpeed = 3;
    [SerializeField] private int dragPanSpeed = 3;
    public int mouseButtonForDrag = 1;   // 0=左键 1=右键 2=中键

    [Header("缩放")]
    [SerializeField] private int zoomSpeed = 3;
    public float minDistance = 10f;
    public float maxDistance = 20f;
    public float initialDistance = 15f;

    [Header("边界留白")]
    public float extraMargin = 2f;
    [Tooltip("初始 Z 偏移量（正值让相机起始位置偏后，更容易看到全网格）")]
    public float initialZOffset = 3f;

    // 计算后的常量
    private float sinAngle;
    private float cosAngle;

    // 运行时状态
    private Vector2 targetPoint;  // XZ 平面上的观察目标点（受网格边界约束）
    private float currentDistance;

    private Vector2 gridMin;
    private Vector2 gridMax;

    private bool playerControl = true; // 是否由玩家控制

    // 聚焦状态
    private bool isFocusing;
    private Transform focusTarget;
    private float focusDuration;
    private float focusTimer;
    private Vector2 focusStartPos;
    private float focusStartDistance;

    // 鼠标拖动状态
    private Vector2 lastMousePos;
    private bool isDragging;

    // PlayerPrefs 键名
    private const string KeyboardPanSpeedKey = "KeyboardPanSpeed";
    private const string DragPanSpeedKey = "DragPanSpeed";
    private const string ZoomSpeedKey = "ZoomSpeed";
    private const string HasLaunchedCameraKey = "HasLaunchedCamera";

    private const int DefaultKeyboardPanSpeed = 3;
    private const int DefaultDragPanSpeed = 3;
    private const int DefaultZoomSpeed = 3;

    // ====================================================================
    //  公开属性（供 UI 读取）
    // ====================================================================

    public int KeyboardPanSpeed => keyboardPanSpeed;
    public int DragPanSpeed => dragPanSpeed;
    public int ZoomSpeed => zoomSpeed;

    // ====================================================================
    //  公开设置方法（供 UI 调用，自动持久化）
    // ====================================================================

    public void SetKeyboardPanSpeed(int value)
    {
        keyboardPanSpeed = Mathf.Clamp(value, 1, 5);
        SaveManager.Instance?.SetInt(KeyboardPanSpeedKey, keyboardPanSpeed);
    }

    public void SetDragPanSpeed(int value)
    {
        dragPanSpeed = Mathf.Clamp(value, 1, 5);
        SaveManager.Instance?.SetInt(DragPanSpeedKey, dragPanSpeed);
    }

    public void SetZoomSpeed(int value)
    {
        zoomSpeed = Mathf.Clamp(value, 1, 5);
        SaveManager.Instance?.SetInt(ZoomSpeedKey, zoomSpeed);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var mainCam = GetComponent<Camera>();
        if (mainCam != null)
            mainCam.tag = "MainCamera";

        // 预计算俯角三角函数值
        float rad = xRotation * Mathf.Deg2Rad;
        sinAngle = Mathf.Sin(rad);
        cosAngle = Mathf.Cos(rad);

        // 固定旋转
        transform.rotation = Quaternion.Euler(xRotation, 0f, 0f);

        // sceneLoaded 只需订阅一次，不依赖 enabled 状态
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

        // 通过事件总线自动响应关卡进出
        GameEventChannel.Register<LevelEnteredEvent>(OnLevelEntered);

        // 初始范围设为空，进入关卡后由 InitializeForLevel 设置
        gridMin = Vector2.zero;
        gridMax = Vector2.zero;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEventChannel.Unregister<LevelEnteredEvent>(OnLevelEntered);
        }
    }

    void Start()
    {
        // 首次启动写入默认值
        var save = SaveManager.Instance;
        if (save != null && !save.GetBool(HasLaunchedCameraKey))
        {
            save.SetInt(KeyboardPanSpeedKey, DefaultKeyboardPanSpeed);
            save.SetInt(DragPanSpeedKey, DefaultDragPanSpeed);
            save.SetInt(ZoomSpeedKey, DefaultZoomSpeed);
            save.SetBool(HasLaunchedCameraKey, true);
        }

        // 读取已保存的值
        keyboardPanSpeed = save?.GetInt(KeyboardPanSpeedKey, DefaultKeyboardPanSpeed) ?? DefaultKeyboardPanSpeed;
        dragPanSpeed = save?.GetInt(DragPanSpeedKey, DefaultDragPanSpeed) ?? DefaultDragPanSpeed;
        zoomSpeed = save?.GetInt(ZoomSpeedKey, DefaultZoomSpeed) ?? DefaultZoomSpeed;
    }

    /// <summary>每次加载新场景时清理场上的多余相机</summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        Camera myCam = GetComponent<Camera>();
        Camera[] allCams = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCams)
        {
            if (cam != myCam && cam.CompareTag("MainCamera"))
                Destroy(cam.gameObject);
        }
        myCam.tag = "MainCamera";
    }

    private void OnLevelEntered(LevelEnteredEvent evt)
    {
        InitializeForLevel();
    }

    void LateUpdate()
    {
        HandleKeyboardPan();
        HandleMouseDrag();
        HandleZoom();
        HandleFocus();
    }

    // ====================================================================
    //  边界
    // ====================================================================

    private void CalculateBounds()
    {
        var grid = GridManager.Instance;
        if (grid?.CurrentLevel == null) return;

        int w = grid.CurrentLevel.width;
        int h = grid.CurrentLevel.height;

        Vector3 c0 = grid.GridToWorld(0, 0);
        Vector3 c1 = grid.GridToWorld(w - 1, h - 1);

        gridMin = new Vector2(
            Mathf.Min(c0.x, c1.x) - extraMargin,
            Mathf.Min(c0.z, c1.z) - extraMargin
        );
        gridMax = new Vector2(
            Mathf.Max(c0.x, c1.x) + extraMargin,
            Mathf.Max(c0.z, c1.z) + extraMargin
        );
    }

    // ====================================================================
    //  平移 — 方向键
    // ====================================================================

    private void HandleKeyboardPan()
    {
        if (!playerControl) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float h = 0f;
        if (keyboard.dKey.isPressed) h += 1f;
        if (keyboard.aKey.isPressed) h -= 1f;
        float v = 0f;
        if (keyboard.wKey.isPressed) v += 1f;
        if (keyboard.sKey.isPressed) v -= 1f;
        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

        Vector2 move = new Vector2(h, v).normalized * 3 * keyboardPanSpeed * Time.deltaTime;
        MoveTarget(move);
    }

    // ====================================================================
    //  平移 — 鼠标拖拽
    // ====================================================================

    private void HandleMouseDrag()
    {
        if (!playerControl) { isDragging = false; return; }

        var mouse = Mouse.current;
        if (mouse == null) return;

        var dragButton = mouseButtonForDrag switch
        {
            0 => mouse.leftButton,
            2 => mouse.middleButton,
            _ => mouse.rightButton,
        };

        if (dragButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePos = mouse.position.ReadValue();
        }
        else if (dragButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (!isDragging) return;

        Vector2 currentMousePos = mouse.position.ReadValue();
        Vector2 delta = (currentMousePos - lastMousePos) * dragPanSpeed * currentDistance/minDistance * Time.deltaTime * 0.1f;

        // 屏幕空间移动 → 世界空间 XZ（反转）
        MoveTarget(new Vector2(-delta.x, -delta.y));

        lastMousePos = currentMousePos;
    }

    /// <summary>移动目标点并约束到网格边界</summary>
    private void MoveTarget(Vector2 delta)
    {
        targetPoint += delta;
        targetPoint.x = Mathf.Clamp(targetPoint.x, gridMin.x, gridMax.x);
        targetPoint.y = Mathf.Clamp(targetPoint.y, gridMin.y, gridMax.y);
        ApplyCameraTransform();
    }

    // ====================================================================
    //  缩放 — 滚轮
    // ====================================================================

    private void HandleZoom()
    {
        if (!playerControl) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        currentDistance -= scroll * 0.005f * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        ApplyCameraTransform();
    }

    // ====================================================================
    //  相机姿态计算
    // ====================================================================

    /// <summary>
    /// 从目标点 + 距离 + 俯角计算相机世界位置
    ///   视线方向 forward = (0, -sinAngle, -cosAngle)
    ///   相机位置 = target - forward × d = (targetX, d×sinAngle, targetY + d×cosAngle)
    /// </summary>
    private void ApplyCameraTransform()
    {
        transform.position = new Vector3(
            targetPoint.x,
            currentDistance * sinAngle,
            targetPoint.y - currentDistance * cosAngle
        );
    }

    // ====================================================================
    //  生命周期 — 由外部管理器调用
    // ====================================================================

    /// <summary>关卡网格加载完成后调用，定位相机到当前网格中心</summary>
    public void InitializeForLevel()
    {
        CalculateBounds();

        currentDistance = initialDistance;

        targetPoint = new Vector2(
            (gridMin.x + gridMax.x) * 0.5f,
            (gridMin.y + gridMax.y) * 0.5f + initialZOffset
        );

        ApplyCameraTransform();
    }

    // ====================================================================
    //  聚焦 — 由外部相机效果系统调用
    // ====================================================================

    /// <summary>
    /// 聚焦到目标，禁用玩家控制，lerp 跟随 duration 秒后恢复控制
    /// </summary>
    public void FocusOnTarget(Transform target, float duration = 1f)
    {
        playerControl = false;
        isFocusing = true;
        focusTarget = target;
        focusDuration = duration;
        focusTimer = 0f;
        focusStartPos = targetPoint;
        focusStartDistance = currentDistance;
    }

    private void HandleFocus()
    {
        if (!isFocusing || focusTarget == null) return;

        focusTimer += Time.deltaTime;
        float t = Mathf.Clamp01(focusTimer / focusDuration);

        // 目标点 lerp 到聚焦对象的 XZ
        var targetXZ = new Vector2(focusTarget.position.x, focusTarget.position.z);
        targetPoint = Vector2.Lerp(focusStartPos, targetXZ, t);

        // 距离 lerp 到最小距离（推近）
        currentDistance = Mathf.Lerp(focusStartDistance, minDistance, t);

        ApplyCameraTransform();

        if (t >= 1f)
        {
            isFocusing = false;
            playerControl = true;
        }
    }

    // ====================================================================
    //  Gizmos
    // ====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 绘制目标点
        Vector3 targetWorld = new Vector3(targetPoint.x, 0f, targetPoint.y);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetWorld, 0.3f);

        // 绘制相机到目标点的连线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetWorld);

        // 绘制网格边界框
        Gizmos.color = Color.green;
        Vector3 bl = new Vector3(gridMin.x, 0, gridMin.y);
        Vector3 br = new Vector3(gridMax.x, 0, gridMin.y);
        Vector3 tl = new Vector3(gridMin.x, 0, gridMax.y);
        Vector3 tr = new Vector3(gridMax.x, 0, gridMax.y);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
