using UnityEngine;
using UnityEngine.UI;

public class MaskAlphaController : MonoBehaviour
{
    public Material guideMaterial;
    public RectTransform targetUI; // 要镂空高亮的目标UI元素

    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (guideMaterial == null)
        {
            Image img = GetComponent<Image>();
            if (img != null) guideMaterial = img.material;
        }
    }

    private void Update()
    {
        if (targetUI == null || guideMaterial == null || canvas == null) return;

        // 1. 获取目标UI元素在Canvas下的世界坐标
        Vector3[] worldCorners = new Vector3[4];
        targetUI.GetWorldCorners(worldCorners);

        // 2. 获取Canvas的RectTransform尺寸
        Rect canvasRect = (canvas.transform as RectTransform).rect;
        float canvasWidth = canvasRect.width;
        float canvasHeight = canvasRect.height;

        // 3. 代码计算镂空区域中心和半径，并将Canvas坐标映射到Shader使用的0~1 UV坐标系
        // 取目标UI四个角的中心点
        Vector2 worldCenter = (worldCorners[0] + worldCorners[2]) / 2f;
        // 取目标UI的宽度的一半作为镂空半径
        Vector2 worldSize = worldCorners[2] - worldCorners[0];
        float radius = Mathf.Min(worldSize.x, worldSize.y) / 2f;

        // 4. 设置Shader参数，将坐标从像素空间转换到0-1的UV空间
        guideMaterial.SetVector("_MaskCenter", worldCenter / new Vector2(canvasWidth, canvasHeight));
        guideMaterial.SetFloat("_MaskRadius", radius / canvasWidth); // 假设宽度和高度的缩放比例一致
        guideMaterial.SetFloat("_MaskSoftness", 0.02f); // 设置边缘柔和度
    }
}