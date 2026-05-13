using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnResumeClicked()
    {
        GameManager.Instance.GameResume();
        UIManager.Instance.Hide("pauseMenu");
    }

    private void OnSettingsClicked()
    {
        UIManager.Instance.Show("settings");
        UIManager.Instance.Hide("pauseMenu");
    }

    private void OnMainMenuClicked()
    {
        GameManager.Instance.BackToMainMenu();
    }
}
