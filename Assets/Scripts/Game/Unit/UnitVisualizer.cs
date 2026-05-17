using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单位可视化管理器 - 负责单位的高亮和悬停效果
/// </summary>
public class UnitVisualizer : MonoBehaviour
{
    [Header("Highlight Materials")]
    [SerializeField] private Material highlightMaterial3D;
    [SerializeField] private Material highlightMaterial2D;

    [Header("Hover Materials (fallback when no Animator)")]
    [SerializeField] private Material hoverMaterial3D;
    [SerializeField] private Material hoverMaterial2D;

    // 高亮记录：Renderer → 原始材质
    private Dictionary<Renderer, Material> highlightedRenderers = new Dictionary<Renderer, Material>();

    // 悬停记录：Renderer → 悬停前的材质（可能是高亮材质，也可能是原始材质）
    private Dictionary<Renderer, Material> hoverOriginalMaterials = new Dictionary<Renderer, Material>();
    private Unit currentHoveredUnit;

    /// <summary>
    /// 高亮指定单位列表
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
                // 保存原始材质
                if (!highlightedRenderers.ContainsKey(renderer))
                {
                    highlightedRenderers[renderer] = renderer.sharedMaterial;
                }
                // 应用高亮材质
                Material mat = renderer is SpriteRenderer ? highlightMaterial2D : highlightMaterial3D;
                if (mat != null)
                    renderer.material = mat;
            }
        }
        Logger.Log($"UnitVisualizer: 高亮 {units.Count} 个单位");
    }

    /// <summary>
    /// 清除所有高亮，恢复原始材质
    /// </summary>
    public void ClearHighlights()
    {
        Debug.Log("[UnitVis] ClearHighlights");
        // 先清除悬停，避免悬停引用已清空的高亮记录
        ClearHoverUnit();

        foreach (var kvp in highlightedRenderers)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
        highlightedRenderers.Clear();
        Logger.Log("UnitVisualizer: 已清除所有高亮");
    }

    /// <summary>
    /// 清除指定单位列表的高亮，保留其他单位的高亮
    /// </summary>
    public void ClearHighlightsOf(List<Unit> unitsToClear)
    {
        if (unitsToClear == null) return;
        var toRemove = new List<Renderer>();
        foreach (var unit in unitsToClear)
        {
            if (unit == null) continue;
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer != null && highlightedRenderers.TryGetValue(renderer, out var original))
                {
                    renderer.material = original;
                    toRemove.Add(renderer);
                }
            }
        }
        foreach (var r in toRemove)
            highlightedRenderers.Remove(r);
    }

    /// <summary>
    /// 设置悬停单位（优先使用 Animator 参数，否则替换材质）
    /// </summary>
    public void SetHoverUnit(Unit unit)
    {
        if (unit == null) return;

        // 清除旧悬停
        ClearHoverUnit();

        currentHoveredUnit = unit;

        // 尝试 Animator 方式
        Animator anim = unit.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Hovered", true);
            return;
        }

        // 备选：替换材质
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // 保存悬停前的材质（可能是高亮材质）
            hoverOriginalMaterials[renderer] = renderer.sharedMaterial;

            Material hoverMat = renderer is SpriteRenderer ? hoverMaterial2D : hoverMaterial3D;
            if (hoverMat != null)
                renderer.material = hoverMat;
        }
    }

    /// <summary>
    /// 清除悬停效果
    /// </summary>
    public void ClearHoverUnit()
    {
        if (currentHoveredUnit == null) return;
        Debug.Log($"[UnitVis] ClearHoverUnit: {currentHoveredUnit.UnitId}");

        // 尝试 Animator 方式
        Animator anim = currentHoveredUnit.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("Hovered", false);
        }
        else
        {
            // 备选：从 hoverOriginalMaterials 恢复材质
            RestoreHoverMaterials(currentHoveredUnit);
        }

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
            if (hoverOriginalMaterials.TryGetValue(renderer, out var preHoverMat))
            {
                renderer.material = preHoverMat;
            }
        }
        hoverOriginalMaterials.Clear();
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
            if (highlightedRenderers.ContainsKey(r))
                return true;
        }
        return false;
    }
}
