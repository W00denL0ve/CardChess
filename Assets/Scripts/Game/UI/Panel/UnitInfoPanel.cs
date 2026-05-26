using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitInfoPanel : MonoBehaviour, IPanelDataReceiver
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI intelligenceText;
    [SerializeField] private TextMeshProUGUI phyDefenseText;
    [SerializeField] private TextMeshProUGUI magDefenseText;
    [SerializeField] private TextMeshProUGUI maxPointText;
    [SerializeField] private Transform buffDisplayArea;
    [SerializeField] private GameObject buffUIPrefab;

    public void OnReceiveData(object data)
    {
        if (data is Unit unit)
        {
            UpdateInfo(unit);
        }
        else
        {
            Logger.LogWarning($"UnitInfoPanel.OnReceiveData: 无法处理数据类型 {data.GetType()}");
        }
    }

    private void UpdateInfo(Unit unit)
    {
        iconImage.sprite = unit.Icon;
        nameText.text = unit.UnitName;
        hpSlider.maxValue = unit.baseValue.maxHealth;
        hpSlider.value = unit.baseValue.currentHealth;
        hpText.text = $"{SolidColors.TxtColor($"{hpSlider.value}", SolidColors.GREEN)}/{SolidColors.TxtColor($"{hpSlider.maxValue}", SolidColors.GREEN)}";
        attackText.text = $"{unit.baseValue.attack}";
        intelligenceText.text = $"{unit.baseValue.intelligence}";
        phyDefenseText.text = $"{unit.baseValue.physicalDefense}";
        magDefenseText.text = $"{unit.baseValue.magicDefense}";
        maxPointText.text = $"{unit.baseValue.movePointLimit}";

        // 刷新 Buff 列表
        // foreach (Transform child in buffDisplayArea)
        //     Destroy(child.gameObject);
        // foreach (var buff in unit.BuffContainer.GetAllBuffs())
        // {
        //     TODO:需先实现buff的UI
        //     var item = Instantiate(buffUIPrefab, buffDisplayArea).GetComponent<BuffUI>();
        //     item.Setup(buff);
        // }
    }
}