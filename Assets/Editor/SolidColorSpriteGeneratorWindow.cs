using UnityEngine;
using UnityEditor;
using System.IO;

public class SolidColorSpriteGeneratorWindow : EditorWindow
{
    private Color selectedColor = Color.white;
    private int pixels = 4;
    private int pixelsPerUnit = 5;

    // 固定默认路径
    private const string DEFAULT_FOLDER = "Assets/Art/Tiles/PureColors";

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
    }

    private void GenerateSolidColorSprite()
    {
        // 确保默认文件夹存在
        if (!AssetDatabase.IsValidFolder(DEFAULT_FOLDER))
        {
            // 递归创建路径中的所有文件夹
            Directory.CreateDirectory(Path.GetDirectoryName(DEFAULT_FOLDER));
            // 由于AssetDatabase需要，最好用AssetDatabase.CreateFolder
            // 简单起见，我们确保上级目录存在，然后创建
            string parent = Path.GetDirectoryName(DEFAULT_FOLDER);
            string folderName = Path.GetFileName(DEFAULT_FOLDER);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                // 创建上级目录（省略详细递归，这里假设上级存在）
                // 实际可靠做法：逐级创建
                Logger.LogError("上级目录不存在，请先创建 Assets/Art/Tiles 文件夹");
                return;
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        // 创建纹理
        Texture2D tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
        Color[] colors = new Color[pixels * pixels];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = selectedColor;
        }
        tex.SetPixels(colors);
        tex.Apply();

        // 生成默认文件名，例如 "Green.png"
        string colorName = ColorUtility.ToHtmlStringRGB(selectedColor); // RRGGBB
        string defaultName = "SolidColor_" + colorName; // 也可以加上 "SolidColor_" 前缀
        string fullPath = Path.Combine(DEFAULT_FOLDER, defaultName + ".png");
        fullPath = fullPath.Replace("\\", "/"); // 统一斜杠

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
}