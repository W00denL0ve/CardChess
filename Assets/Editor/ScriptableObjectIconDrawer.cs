using UnityEngine;
using UnityEditor;

/// <summary>
/// 在 Project 窗口中为 ScriptableObject 显示自定义图标（基于其 icon/artwork 字段）
/// </summary>
[InitializeOnLoad]
public static class ScriptableObjectIconDrawer
{
    static ScriptableObjectIconDrawer()
    {
        EditorApplication.projectWindowItemOnGUI += DrawIcon;
    }

    private static void DrawIcon(string guid, Rect selectionRect)
    {
        // 缩略图太小不绘制（列表模式）
        if (selectionRect.height < 20) return;

        // 通过路径和扩展名快速过滤（.asset 才可能是 SO）
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset")) return;

        // 只处理特定文件夹下的资产，避免扫描全工程
        if (!path.StartsWith("Assets/")) return;

        // 加载主资产
        ScriptableObject so = AssetDatabase.LoadMainAssetAtPath(path) as ScriptableObject;
        if (so == null) return;

        Texture tex = GetIconTexture(so);
        if (tex == null) return;

        // 在缩略图中间绘制小图标（不覆盖原图标）
        float size = selectionRect.height * 0.6f;
        float offset = (selectionRect.height - size) * 0.5f;
        Rect iconRect = new Rect(selectionRect.x + offset, selectionRect.y + offset,
                                 size, size);
        GUI.DrawTexture(iconRect, tex, ScaleMode.ScaleToFit);
    }

    private static Texture GetIconTexture(ScriptableObject so)
    {
        // CardData → artwork
        if (so is CardData card && card.artwork != null)
            return card.artwork.texture;

        // UnitConfig → icon
        if (so is UnitConfig unit && unit.icon != null)
            return unit.icon.texture;

        // Effect → icon
        if (so is Effect effect && effect.icon != null)
            return effect.icon.texture;

        return null;
    }
}
