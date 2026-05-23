using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : Editor
{
    private SerializedProperty colorPresetProp;
    private SerializedProperty cardColorProp;
    private SerializedProperty chainsProp;

    private void OnEnable()
    {
        colorPresetProp = serializedObject.FindProperty("colorPreset");
        cardColorProp = serializedObject.FindProperty("cardColor");
        chainsProp = serializedObject.FindProperty("chains");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制默认字段（排除颜色和 chains）
        DrawPropertiesExcluding(serializedObject, "colorPreset", "cardColor", "m_Script", "chains");

        // 颜色区域
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

        // 效果链区域
        EditorGUILayout.Space();
        GUILayout.Label("效果链", EditorStyles.boldLabel);

        // 使用默认的 PropertyField 绘制列表（可以展开每个 EffectChain 的内部 steps）
        EditorGUILayout.PropertyField(chainsProp, new GUIContent("效果链列表"), true);

        // 自定义添加按钮（可选），Unity 默认列表也会有一个“+”按钮，但您可以保留这个更明确的按钮
        // if (GUILayout.Button("+ 添加新链", GUILayout.Height(25)))
        // {
        //     AddNewChain();
        // }

        serializedObject.ApplyModifiedProperties();
    }

    // private void AddNewChain()
    // {
    //     // 由于 EffectChain 是普通可序列化类，arraySize++ 会自动创建新的 EffectChain 实例
    //     // 并且该实例独立于其他任何链（因为是内联序列化）
    //     chainsProp.arraySize++;
    // }

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