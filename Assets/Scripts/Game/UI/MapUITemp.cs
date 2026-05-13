using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个临时的地图UI，后续做好了肉鸽系统后替换
/// </summary>
public class MapUITemp : MonoBehaviour
{
    [SerializeField] private Button level1Button;
    // Start is called before the first frame update
    void Start()
    {
        if(level1Button != null)
            level1Button.onClick.AddListener(OnLevel1Clicked);
    }

    private void OnLevel1Clicked()
    {
        GameManager.Instance.EnterLevel("testLevel");
    }
}
