using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    private SerializedProperty colorPresetProp;
    private SerializedProperty cardColorProp;
    private CardColorPreset prevPreset;

    private void OnEnable()
    {
        colorPresetProp = serializedObject.FindProperty("colorPreset");
        cardColorProp = serializedObject.FindProperty("cardColor");
        prevPreset = (CardColorPreset)colorPresetProp.enumValueIndex;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 默认字段
        DrawPropertiesExcluding(serializedObject,
            "colorPreset", "cardColor", "m_Script");

        // ── 颜色区域 ──
        EditorGUILayout.Space();
        GUILayout.Label("卡牌颜色", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(colorPresetProp, new GUIContent("颜色预设"));
        if (EditorGUI.EndChangeCheck())
        {
            ApplyPresetColor();
            serializedObject.ApplyModifiedProperties();
        }

        EditorGUILayout.PropertyField(cardColorProp, new GUIContent("自定义颜色"));

        serializedObject.ApplyModifiedProperties();
    }

    private void ApplyPresetColor()
    {
        var preset = (CardColorPreset)colorPresetProp.enumValueIndex;
        Color color = preset switch
        {
            CardColorPreset.Red   => new Color(0.9f, 0.2f, 0.2f),
            CardColorPreset.Green => new Color(0.2f, 0.8f, 0.2f),
            CardColorPreset.Blue  => new Color(0.2f, 0.4f, 0.9f),
            _ => cardColorProp.colorValue
        };
        if (preset != CardColorPreset.None)
            cardColorProp.colorValue = color;
    }
}
