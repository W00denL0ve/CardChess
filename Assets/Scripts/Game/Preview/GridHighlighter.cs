using System.Collections.Generic;
using UnityEngine;

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
            GameObject highlight = Instantiate(highlightPrefab, cell.transform.position, Quaternion.identity);
            highlights.Add(highlight);
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