using UnityEngine;
using UnityEditor;
using System.IO;

public class SolidColorSpriteGeneratorWindow : EditorWindow
{
    private Color selectedColor = Color.white;
    private int pixels = 4;
    private int pixelsPerUnit = 5;

    // 发光精灵参数
    private Color glowColor = new Color(1f, 0.843f, 0f);
    private int glowSize = 128;
    private int glowPPU = 100;
    private float glowFalloff = 2f;
    private bool roundedRect = true;
    private float cornerRadius = 0.2f;  // 相对尺寸的比例
    private float aspectRatio = 0.7f;   // 宽/高，<1 竖长

    // 固定默认路径
    private const string DEFAULT_FOLDER = "Assets/Art/Tiles/PureColors";
    private const string GLOW_FOLDER = "Assets/Art/UI/GlowTextures";

    [MenuItem("Tools/Solid Color Sprite Generator")]
    public static void ShowWindow()
    {
        GetWindow<SolidColorSpriteGeneratorWindow>("纯色精灵生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("纯色精灵生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        selectedColor = EditorGUILayout.ColorField("选择颜色", selectedColor);
        pixels = EditorGUILayout.IntField("n_Pixels(n*n)", pixels);
        pixelsPerUnit = EditorGUILayout.IntField("PPU (Pixels Per Unit)", pixelsPerUnit);
        if (pixelsPerUnit < 1) pixelsPerUnit = 1;

        EditorGUILayout.Space();

        if (GUILayout.Button("生成纯色精灵", GUILayout.Height(30)))
        {
            GenerateSolidColorSprite();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        GUILayout.Label("径向渐变发光精灵", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        glowColor = EditorGUILayout.ColorField("发光颜色", glowColor);
        glowSize = EditorGUILayout.IntField("纹理尺寸", glowSize);
        glowPPU = EditorGUILayout.IntField("PPU", glowPPU);
        glowFalloff = EditorGUILayout.Slider("衰减指数", glowFalloff, 0.5f, 4f);
        roundedRect = EditorGUILayout.Toggle("圆角矩形", roundedRect);
        if (roundedRect)
        {
            cornerRadius = EditorGUILayout.Slider("圆角比例", cornerRadius, 0.05f, 0.5f);
            aspectRatio = EditorGUILayout.Slider("宽高比", aspectRatio, 0.3f, 1f);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("生成发光精灵", GUILayout.Height(30)))
        {
            GenerateGlowSprite();
        }
    }

    private void GenerateGlowSprite()
    {
        EnsureFolderExists(GLOW_FOLDER);

        Texture2D tex = new Texture2D(glowSize, glowSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[glowSize * glowSize];
        float half = glowSize * 0.5f;
        float margin = half * 0.18f;                 // 留出发光渐变空间
        float cr = half * cornerRadius;               // 圆角像素半径
        float hw = (half - margin) * aspectRatio;     // 半宽（缩进）
        float hh = half - margin;                     // 半高（缩进）

        // 软过渡区间：从矩形内部开始淡出
        float glowStart = -margin * 0.25f;
        float glowRange = margin * 1.2f;

        for (int y = 0; y < glowSize; y++)
        {
            for (int x = 0; x < glowSize; x++)
            {
                float alpha;
                if (roundedRect)
                {
                    // 标准圆角矩形 SDF
                    float px = Mathf.Abs(x - half) - hw + cr;
                    float py = Mathf.Abs(y - half) - hh + cr;
                    float sdf = Mathf.Sqrt(Mathf.Max(px, 0) * Mathf.Max(px, 0) + Mathf.Max(py, 0) * Mathf.Max(py, 0)) - cr;

                    float t = Mathf.Clamp01((sdf - glowStart) / glowRange);
                    alpha = 1f - t * t * (3f - 2f * t); // smoothstep
                }
                else
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                    alpha = Mathf.Clamp01(1f - dist);
                }
                alpha = Mathf.Pow(alpha, glowFalloff);
                pixels[y * glowSize + x] = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string colorName = ColorUtility.ToHtmlStringRGB(glowColor);
        string defaultName = $"Glow_{colorName}_{glowSize}";

        string path = EditorUtility.SaveFilePanelInProject(
            "保存发光精灵", defaultName, "png", "选择保存路径", GLOW_FOLDER);

        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(tex);
            return;
        }

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = glowPPU;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        DestroyImmediate(tex);

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            Selection.activeObject = sprite;

        Logger.Log($"发光精灵已生成：{path}");
    }

    private void GenerateSolidColorSprite()
    {
        EnsureFolderExists(DEFAULT_FOLDER);

        // 创建纹理
        Texture2D tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
        Color[] colors = new Color[pixels * pixels];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = selectedColor;
        }
        tex.SetPixels(colors);
        tex.Apply();

        // 生成默认文件名
        string colorName = ColorUtility.ToHtmlStringRGB(selectedColor);
        string defaultName = "SolidColor_" + colorName;

        // 打开保存面板，路径固定到默认文件夹
        string path = EditorUtility.SaveFilePanelInProject(
            "保存纯色精灵",
            defaultName,
            "png",
            "选择保存路径",
            DEFAULT_FOLDER); // 这行指定默认文件夹

        if (string.IsNullOrEmpty(path))
        {
            DestroyImmediate(tex);
            return;
        }

        // 写入PNG
        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        // 设置导入参数
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        DestroyImmediate(tex);

        // 选中生成的精灵
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            Selection.activeObject = sprite;

        Logger.Log($"纯色精灵已生成：{path}");
    }

    /// <summary>递归确保 AssetDatabase 文件夹存在</summary>
    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);
        EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}