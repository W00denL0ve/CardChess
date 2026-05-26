using UnityEngine;
using UnityEngine.UI;

public class HUDUI : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button inventoryButton;

    private void Awake()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
        if (mapButton != null)
            mapButton.onClick.AddListener(OnMapClicked);
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryClicked);
        GameEventChannel.Register<LevelEnteredEvent>(OnLevelEntered);
        GameEventChannel.Register<MapEnteredEvent>(OnMapEntered);
    }

    private void OnDestroy()
    {
        GameEventChannel.Unregister<LevelEnteredEvent>(OnLevelEntered);
        GameEventChannel.Unregister<MapEnteredEvent>(OnMapEntered);
    }

    private void OnPauseClicked()
    {
        GameManager.Instance.GamePause();
        UIManager.Instance.Show("pauseMenu");
        // ...
    }

    private void OnMapClicked()
    {
        if (UIManager.Instance.IsShown("map"))
            UIManager.Instance.Hide("map");
        else
            UIManager.Instance.Show("map");
        // ...
    }

    private void OnInventoryClicked()
    {
        // UIManager.Instance.Show("inventory"); //todo
        // ...
    }

    public void OnMapEntered(MapEnteredEvent e)
    {
        Logger.Log("收到地图进入消息");
        mapButton.interactable = false;
    }

    public void OnLevelEntered(LevelEnteredEvent e)
    {
        mapButton.interactable = true;
    }
}
