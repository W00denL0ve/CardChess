using UnityEngine;
using UnityEngine.UI;

public class TestUnitVisual : MonoBehaviour
{
    public Slider healthBar;
    public SpriteRenderer bodyRenderer;
    public Text nameText;
    private Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();
        if (unit != null)
        {
            UpdateName(unit.UnitId);
            UpdateHealth(unit.CurrentHealth, unit.MaxHealth);
        }
        // 监听事件
        GameEventChannel.Register<UnitHealthChangedEvent>(OnHealthChanged);
        GameEventChannel.Register<UnitDeathEvent>(OnDeath);
        GameEventChannel.Register<UnitMovedEvent>(OnMoved);
    }

    void UpdateName(string id) { if (nameText) nameText.text = id; }

    void UpdateHealth(int current, int max)
    {
        if (healthBar != null)
        {
            healthBar.maxValue = max;
            healthBar.value = current;
        }
    }

    void OnHealthChanged(UnitHealthChangedEvent evt)
    {
        if (evt.Unit == unit) UpdateHealth(evt.NewHealth, evt.MaxHealth);
    }

    void OnDeath(UnitDeathEvent evt)
    {
        if (evt.Unit == unit)
        {
            bodyRenderer.color = Color.gray; // 变灰
            healthBar.gameObject.SetActive(false);
        }
    }

    void OnMoved(UnitMovedEvent evt)
    {
        if (evt.Unit == unit)
            transform.position = GridManager.Instance.GridToWorld(evt.To);
    }
}