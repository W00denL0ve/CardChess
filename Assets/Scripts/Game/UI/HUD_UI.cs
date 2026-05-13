using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button inventoryButton;

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
        if (mapButton != null)
            mapButton.onClick.AddListener(OnMapClicked);
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryClicked);
    }

    private void OnPauseClicked()
    {
        GameManager.Instance.GamePause();
        UIManager.Instance.Show("pauseMenu");
        // ...
    }

    private void OnMapClicked()
    {
        UIManager.Instance.Show("map");
        // ...
    }

    private void OnInventoryClicked()
    {
        // UIManager.Instance.Show("inventory"); //todo
        // ...
    }
}
