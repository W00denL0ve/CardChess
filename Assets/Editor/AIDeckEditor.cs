using UnityEngine;
using UnityEditor;

/// <summary>
/// 自定义 AI 行为配置（AIDeck）的 Inspector 面板
/// 提供效果链条目的增删、预设快速配置、深度克隆等功能
/// </summary>
[CustomEditor(typeof(AIDeck))]
public class AIDeckEditor : Editor
{
    private int selectedPresetIndex = 0;

    // 存储自定义预设的值（除 chain 外的所有字段）
    private int customEnergyCost = 1;
    private int customCooldown = 1;
    private int customMaxUsePerBattle = 0;
    private int customTargetType = (int)AITargetType.Hostile;
    private int customCategory = (int)ChainCategory.Attack;
    private int customBaseScore = 0;
    private string customEditorLabel = "自定义";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("energyPerTurn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("strategy"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("效果链", EditorStyles.boldLabel);

        var entries = serializedObject.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            DrawEntry(entry, i, entries);
        }

        if (GUILayout.Button("+ 添加条目"))
        {
            AddNewEntry(entries);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntry(SerializedProperty entry, int index, SerializedProperty entries)
    {
        EditorGUILayout.BeginVertical("box");

        var editorLabelProp = entry.FindPropertyRelative("editorLabel");
        string displayName;
        if (editorLabelProp != null && !string.IsNullOrEmpty(editorLabelProp.stringValue))
            displayName = $"{editorLabelProp.stringValue} {index}";
        else
            displayName = $"条目 {index}";

        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, displayName, true);

        if (entry.isExpanded)
        {
            EditorGUI.indentLevel++;

            var chainProp = entry.FindPropertyRelative("chain");
            EditorGUILayout.PropertyField(chainProp, new GUIContent("效果链"));

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("energyCost"), new GUIContent("消耗能量"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldown"), new GUIContent("冷却"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxUsePerBattle"), new GUIContent("最大次数"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("targetType"), new GUIContent("目标类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("category"), new GUIContent("链类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("baseScore"), new GUIContent("基础分"));

            DrawPresetUI(entry);

            // 操作按钮行：删除 + 保存自定义预设
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("删除此条目", GUILayout.Width(90)))
            {
                if (EditorUtility.DisplayDialog("删除条目", "确定删除该条目吗？", "删除", "取消"))
                {
                    entries.DeleteArrayElementAtIndex(index);
                }
            }
            if (GUILayout.Button("保存为自定义预设", GUILayout.Width(120)))
            {
                SaveAsCustomPreset(entry);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPresetUI(SerializedProperty entry)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("预设", GUILayout.Width(40));

        string[] presetNames = { "普通攻击", "特殊攻击", "友方增幅", "敌方减益", "友方治疗", "自定义" };
        selectedPresetIndex = EditorGUILayout.Popup(selectedPresetIndex, presetNames);

        if (GUILayout.Button("应用", GUILayout.Width(50)))
        {
            ApplyPreset(entry, (PresetType)selectedPresetIndex);
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void AddNewEntry(SerializedProperty entries)
    {
        entries.arraySize++;
        var newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);

        // 设置基础默认值
        newEntry.FindPropertyRelative("energyCost").intValue = 1;
        newEntry.FindPropertyRelative("cooldown").intValue = 1;
        newEntry.FindPropertyRelative("maxUsePerBattle").intValue = 0;
        newEntry.FindPropertyRelative("targetType").enumValueIndex = (int)AITargetType.Hostile;
        newEntry.FindPropertyRelative("category").enumValueIndex = (int)ChainCategory.Attack;
        newEntry.FindPropertyRelative("baseScore").intValue = 10;

        // 清空编辑器标签
        var editorLabelProp = newEntry.FindPropertyRelative("editorLabel");
        if (editorLabelProp != null) editorLabelProp.stringValue = "";

        // ★ 关键：创建一个全新的 EffectChain 实例（空 steps），避免与任何现有条目共享
        var chainProp = newEntry.FindPropertyRelative("chain");
        var newChain = new EffectChain();
        newChain.steps = new System.Collections.Generic.List<ChainStep>();
        AssignSerializableObjectToProperty(chainProp, newChain);
    }

    /// <summary>
    /// 将当前条目的配置（除了 chain）保存为自定义预设
    /// </summary>
    private void SaveAsCustomPreset(SerializedProperty entry)
    {
        customEnergyCost = entry.FindPropertyRelative("energyCost").intValue;
        customCooldown = entry.FindPropertyRelative("cooldown").intValue;
        customMaxUsePerBattle = entry.FindPropertyRelative("maxUsePerBattle").intValue;
        customTargetType = entry.FindPropertyRelative("targetType").enumValueIndex;
        customCategory = entry.FindPropertyRelative("category").enumValueIndex;
        customBaseScore = entry.FindPropertyRelative("baseScore").intValue;

        var labelProp = entry.FindPropertyRelative("editorLabel");
        if (labelProp != null && !string.IsNullOrEmpty(labelProp.stringValue))
            customEditorLabel = labelProp.stringValue;
        else
            customEditorLabel = "自定义";

        EditorUtility.DisplayDialog("自定义预设", $"已保存当前配置为自定义预设。\n标签：{customEditorLabel}", "确定");
    }

    /// <summary>
    /// 将任意可序列化对象赋值给 SerializedProperty（支持 [SerializeReference] 或普通可序列化类）
    /// 注意：如果字段未标记 [SerializeReference]，此方法可能无法正常工作。
    /// 建议将 AIChainEntry 中的 public EffectChain chain; 改为 [SerializeReference] public EffectChain chain;
    /// </summary>
    private void AssignSerializableObjectToProperty(SerializedProperty prop, object value)
    {
        // 方法1：使用 managedReferenceValue（需要字段标记 [SerializeReference]）
        // 如果 prop 的 propertyType 是 ManagedReference，则可以直接赋值
        if (prop.propertyType == SerializedPropertyType.ManagedReference)
        {
            prop.managedReferenceValue = value;
            return;
        }

        // 方法2：降级方案 – 通过 JSON 再写入（可靠性较低，但适用于没有 [SerializeReference] 的普通对象）
        // 注意：此方法不能用于数组元素等复杂情况，仅用于保底。
        var json = JsonUtility.ToJson(value);
        var tempObj = ScriptableObject.CreateInstance<SerializationHelper>();
        tempObj.jsonData = json;
        var tempSerialized = new SerializedObject(tempObj);
        var tempProp = tempSerialized.FindProperty("jsonData");
        // 将 JSON 字符串写入目标属性（需要目标字段是 string 类型，显然不通用）
        // 因此不推荐使用。最好在 AIChainEntry 中为 chain 添加 [SerializeReference]。
        Debug.LogWarning("AssignSerializableObjectToProperty 降级方案未实现，请为 chain 字段添加 [SerializeReference] 属性。");
    }

    // 辅助类，仅用于临时存储 JSON（不是最终方案的一部分）
    private class SerializationHelper : ScriptableObject { public string jsonData; }

    private enum PresetType
    {
        NormalAttack,
        SpecialAttack,
        AllyBuff,
        EnemyDebuff,
        Heal,
        Custom
    }

    private void ApplyPreset(SerializedProperty entry, PresetType preset)
    {
        var energyCost = entry.FindPropertyRelative("energyCost");
        var cooldown = entry.FindPropertyRelative("cooldown");
        var maxUse = entry.FindPropertyRelative("maxUsePerBattle");
        var targetType = entry.FindPropertyRelative("targetType");
        var category = entry.FindPropertyRelative("category");
        var baseScore = entry.FindPropertyRelative("baseScore");
        var editorLabelProp = entry.FindPropertyRelative("editorLabel");

        switch (preset)
        {
            case PresetType.NormalAttack:
                energyCost.intValue = 1;
                cooldown.intValue = 1;
                maxUse.intValue = 0;
                targetType.enumValueIndex = (int)AITargetType.Hostile;
                category.enumValueIndex = (int)ChainCategory.Attack;
                baseScore.intValue = 0;
                if (editorLabelProp != null) editorLabelProp.stringValue = "普通攻击";
                break;
            case PresetType.SpecialAttack:
                energyCost.intValue = 3;
                cooldown.intValue = 1;
                maxUse.intValue = 0;
                targetType.enumValueIndex = (int)AITargetType.Hostile;
                category.enumValueIndex = (int)ChainCategory.Attack;
                baseScore.intValue = 5;
                if (editorLabelProp != null) editorLabelProp.stringValue = "特殊攻击";
                break;
            case PresetType.AllyBuff:
                energyCost.intValue = 2;
                cooldown.intValue = 1;
                maxUse.intValue = 0;
                targetType.enumValueIndex = (int)AITargetType.Ally_Self;
                category.enumValueIndex = (int)ChainCategory.Buff;
                baseScore.intValue = 5;
                if (editorLabelProp != null) editorLabelProp.stringValue = "友方增幅";
                break;
            case PresetType.EnemyDebuff:
                energyCost.intValue = 2;
                cooldown.intValue = 1;
                maxUse.intValue = 0;
                targetType.enumValueIndex = (int)AITargetType.Hostile;
                category.enumValueIndex = (int)ChainCategory.Debuff;
                baseScore.intValue = 5;
                if (editorLabelProp != null) editorLabelProp.stringValue = "敌方减益";
                break;
            case PresetType.Heal:
                energyCost.intValue = 2;
                cooldown.intValue = 2;
                maxUse.intValue = 0;
                targetType.enumValueIndex = (int)AITargetType.Ally_Self;
                category.enumValueIndex = (int)ChainCategory.Heal;
                baseScore.intValue = 0;
                if (editorLabelProp != null) editorLabelProp.stringValue = "友方治疗";
                break;
            case PresetType.Custom:
                // 应用自定义预设
                energyCost.intValue = customEnergyCost;
                cooldown.intValue = customCooldown;
                maxUse.intValue = customMaxUsePerBattle;
                targetType.enumValueIndex = customTargetType;
                category.enumValueIndex = customCategory;
                baseScore.intValue = customBaseScore;
                if (editorLabelProp != null) editorLabelProp.stringValue = customEditorLabel;
                return;
        }

        serializedObject.ApplyModifiedProperties();
    }
}