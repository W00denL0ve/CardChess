using System.Collections.Generic;
using UnityEngine;

public class PreviewManager : MonoBehaviour
{
    public static PreviewManager Instance { get; private set; }

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

    public void PreviewCard(CardData card)
    {
        List<Cell> previewCells = new List<Cell>();
        List<Character> sources = new List<Character>();

        //todo 根据card.effects中的EffectContext生成预览范围和来源列表

        GridHighlighter.Instance.HighlightCells(previewCells);
        OverlayManager.Instance.ShowOverlayExcept(sources);
    }

    public void ClearPreview()
    {
        GridHighlighter.Instance.ClearHighlights();
        OverlayManager.Instance.HideOverlay();
    }
}