using System;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("已弃用，请使用GridVisualizer实现")]
/// <summary>
/// 格子预览与高亮。已弃用，逻辑在GridVisualizer中实现
/// </summary>
public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance { get; private set; }

    public GameObject highlightPrefab;
    private List<GameObject> highlights = new List<GameObject>();

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
    }

    public void HighlightCells(List<Cell> cells)
    {
        ClearHighlights();
        foreach (var cell in cells)
        {
            // GameObject highlight = Instantiate(highlightPrefab, cell.transform.position, Quaternion.identity);
            // highlights.Add(highlight);
        }
    }

    public void ClearHighlights()
    {
        foreach (var h in highlights)
        {
            Destroy(h);
        }
        highlights.Clear();
    }
}