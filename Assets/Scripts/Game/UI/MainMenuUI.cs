using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button encyclopediaButton;

    private void Start()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (encyclopediaButton != null)
            encyclopediaButton.onClick.AddListener(OnEncyclopediaClicked);
    }

    private void OnQuitClicked()
    {
        GameManager.Instance.QuitGame();
        // 如果需要在退出前做一些事情（比如提示保存），可以在这里添加
    }

    private void OnStartClicked()
    {
        GameManager.Instance.StartNewGame();
        // 可以在这里添加一些转场动画或者音效
    }

    private void OnSettingsClicked()
    {
        UIManager.Instance.Show("settings");
        // 可以在这里添加一些转场动画或者音效
    }

    private void OnEncyclopediaClicked()
    {
        UIManager.Instance.Show("encyclopedia");
        // 可以在这里添加一些转场动画或者音效
    }
}
