using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private Button saveAndExit;

    private void Start()
    {
        if (saveAndExit != null)
            saveAndExit.onClick.AddListener(OnSaveClicked);
    }

    private void OnSaveClicked()
    {
        UIManager.Instance.Hide("settings");
    }
}