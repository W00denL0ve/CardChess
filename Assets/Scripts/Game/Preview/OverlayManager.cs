using System.Collections.Generic;
using UnityEngine;

public class OverlayManager : MonoBehaviour
{
    public static OverlayManager Instance { get; private set; }

    public GameObject overlayCanvas;

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

    public void ShowOverlayExcept(Unit exceptUnit)
    {
        overlayCanvas.SetActive(true);
        // Set layer or mask to exclude exceptUnit
    }

    public void ShowOverlayExcept(List<Unit> exceptUnits)
    {
        overlayCanvas.SetActive(true);
        // Logic to exclude multiple units, e.g., set layers or masks
    }

    public void HideOverlay()
    {
        overlayCanvas.SetActive(false);
    }
}