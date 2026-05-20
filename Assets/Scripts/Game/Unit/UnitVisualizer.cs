using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位可视化管理器 - 负责单位的高亮和悬停效果
/// 2D Sprite → 切换 Sorting Layer；3D → 替换材质
/// </summary>
public class UnitVisualizer : MonoBehaviour
{
    public static UnitVisualizer Instance { get; private set; }

    void Awake() { Instance = this; }

    [Header("3D Highlight")]
    [SerializeField] private Material highlightMaterial3D;

    [Header("2D Highlight (Sorting Layer)")]
    [SerializeField] private string highlightSortingLayer = "Highlight";

    [Header("3D Hover (fallback when no Animator)")]
    [SerializeField] private Material hoverMaterial3D;

    [Header("2D Hover (fallback when no Animator)")]
    public Color hoverColor = new Color(1f, 1f, 0.6f, 1f);
    [Range(0f, 1f)]
    public float hoverLerp = 0.3f;

    // 3D 高亮记录：Renderer → 原始材质
    private Dictionary<Renderer, Material> highlighted3D = new Dictionary<Renderer, Material>();

    // 2D 高亮记录：SpriteRenderer → 原始排序信息
    private Dictionary<SpriteRenderer, (string layer, int order)> highlighted2D = new Dictionary<SpriteRenderer, (string, int)>();

    // 3D 悬停记录：Renderer → 原始材质
    private Dictionary<Renderer, Material> hoverOriginalMaterials = new Dictionary<Renderer, Material>();

    // 2D 悬停记录：SpriteRenderer → 原始颜色
    private Dictionary<SpriteRenderer, Color> hoverOriginalColors = new Dictionary<SpriteRenderer, Color>();

    private Unit currentHoveredUnit;

    /// <summary>
    /// 高亮指定单位列表
    /// 2D Sprite → 切到 Highlight 层；
    /// 3D → 替换材质
    /// </summary>
    public void HighlightUnits(List<Unit> units)
    {
        foreach (var unit in units)
        {
            if (unit == null || !unit.IsAlive) continue;

            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                if (renderer is SpriteRenderer sr)
                {
                    // 2D：保存排序信息 → 切到高亮层
                    if (!highlighted2D.ContainsKey(sr))
                        highlighted2D[sr] = (sr.sortingLayerName, sr.sortingOrder);
                    sr.sortingLayerName = highlightSortingLayer;
                }
                else
                {
                    // 3D：保存材质 → 替换高亮材质
                    if (!highlighted3D.ContainsKey(renderer))
                        highlighted3D[renderer] = renderer.sharedMaterial;
                    if (highlightMaterial3D != null)
                        renderer.material = highlightMaterial3D;
                }
            }
        }
        // Logger.Log($"UnitVisualizer: 高亮 {units.Count} 个单位");
    }

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    public void ClearHighlights()
    {
        Debug.Log("[UnitVis] ClearHighlights");
        ClearHoverUnit();

        // 恢复 3D 材质
        foreach (var kvp in highlighted3D)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
        highlighted3D.Clear();

        // 恢复 2D 排序
        foreach (var kvp in highlighted2D)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sortingLayerName = kvp.Value.layer;
                kvp.Key.sortingOrder = kvp.Value.order;
            }
        }
        highlighted2D.Clear();

        Logger.Log("UnitVisualizer: 已清除所有高亮");
    }

    /// <summary>
    /// 清除指定单位列表的高亮，保留其他单位的高亮
    /// </summary>
    public void ClearHighlightsOf(List<Unit> unitsToClear)
    {
        if (unitsToClear == null) return;

        var toRemove3D = new List<Renderer>();
        var toRemove2D = new List<SpriteRenderer>();

        foreach (var unit in unitsToClear)
        {
            if (unit == null) continue;

            // 恢复 3D
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer is SpriteRenderer sr)
                {
                    if (highlighted2D.TryGetValue(sr, out var info))
                    {
                        sr.sortingLayerName = info.layer;
                        sr.sortingOrder = info.order;
                        toRemove2D.Add(sr);
                    }
                }
                else
                {
                    if (renderer != null && highlighted3D.TryGetValue(renderer, out var original))
                    {
                        renderer.material = original;
                        toRemove3D.Add(renderer);
                    }
                }
            }
        }

        foreach (var r in toRemove3D) highlighted3D.Remove(r);
        foreach (var r in toRemove2D) highlighted2D.Remove(r);
    }

    /// <summary>
    /// 设置悬停单位 — 2D 改颜色，3D 改材质
    /// </summary>
    public void SetHoverUnit(Unit unit)
    {
        if (unit == null) return;

        ClearHoverUnit();
        currentHoveredUnit = unit;

        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            if (renderer is SpriteRenderer sr)
            {
                if (!hoverOriginalColors.ContainsKey(sr))
                    hoverOriginalColors[sr] = sr.color;
                sr.color = Color.Lerp(sr.color, hoverColor, hoverLerp);
            }
            else
            {
                hoverOriginalMaterials[renderer] = renderer.sharedMaterial;
                if (hoverMaterial3D != null)
                    renderer.material = hoverMaterial3D;
            }
        }
    }

    /// <summary>
    /// 清除悬停效果
    /// </summary>
    public void ClearHoverUnit()
    {
        if (currentHoveredUnit == null) return;
        Debug.Log($"[UnitVis] ClearHoverUnit: {currentHoveredUnit.UnitId}");

        RestoreHoverMaterials(currentHoveredUnit);
        currentHoveredUnit = null;
    }

    /// <summary>
    /// 恢复悬停单位的材质（回到高亮或原始）
    /// </summary>
    void RestoreHoverMaterials(Unit unit)
    {
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            if (renderer is SpriteRenderer sr)
            {
                // 2D：恢复原始颜色
                if (hoverOriginalColors.TryGetValue(sr, out var origColor))
                    sr.color = origColor;
            }
            else
            {
                // 3D：恢复原始材质
                if (hoverOriginalMaterials.TryGetValue(renderer, out var preMat))
                    renderer.material = preMat;
            }
        }
        hoverOriginalMaterials.Clear();
        hoverOriginalColors.Clear();
    }

    /// <summary>
    /// 检查指定单位是否处于高亮状态
    /// </summary>
    public bool IsHighlighted(Unit unit)
    {
        if (unit == null) return false;
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is SpriteRenderer sr && highlighted2D.ContainsKey(sr))
                return true;
            if (highlighted3D.ContainsKey(r))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 清除所有预览视觉（高亮 + 悬停）
    /// </summary>
    public void ClearAll()
    {
        ClearHighlights();
        ClearHoverUnit();
    }
}
