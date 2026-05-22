using UnityEngine;
using UnityEditor;

/// <summary>
/// 自定义 AI 行为配置（AIDeck）的 Inspector 面板
/// 提供效果链条目的增删、预设快速配置、空链警告等功能
/// </summary>
[CustomEditor(typeof(AIDeck))]
public class AIDeckEditor : Editor
{
    // 记录当前在下拉列表中选择的预设索引（仅用于 UI，不序列化）
    private int selectedPresetIndex = 0;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制基础字段
        EditorGUILayout.PropertyField(serializedObject.FindProperty("energyPerTurn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("strategy"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("效果链", EditorStyles.boldLabel);

        // 获取 entries 列表属性
        var entries = serializedObject.FindProperty("entries");

        // 遍历绘制每个条目
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            DrawEntry(entry, i, entries);
        }

        // 添加新条目的按钮
        if (GUILayout.Button("+ 添加条目"))
        {
            AddNewEntry(entries);
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 绘制单个条目（带折叠框）
    /// </summary>
    /// <param name="entry">条目的 SerializedProperty</param>
    /// <param name="index">条目索引</param>
    /// <param name="entries">整个 entries 列表属性，用于删除操作</param>
    private void DrawEntry(SerializedProperty entry, int index, SerializedProperty entries)
    {
        EditorGUILayout.BeginVertical("box");

        // 获取编辑器标签（用于友好显示名称）
        var editorLabelProp = entry.FindPropertyRelative("editorLabel");
        string displayName;
        if (editorLabelProp != null && !string.IsNullOrEmpty(editorLabelProp.stringValue))
            displayName = $"{editorLabelProp.stringValue} {index}";
        else
            displayName = $"条目 {index}";

        // 折叠标题栏
        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, displayName, true);

        // 如果折叠打开，则显示详细字段
        if (entry.isExpanded)
        {
            EditorGUI.indentLevel++;

            // 效果链字段（不可被预设覆盖）
            var chainProp = entry.FindPropertyRelative("chain");
            EditorGUILayout.PropertyField(chainProp, new GUIContent("效果链"));

            // 其他数值字段
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("energyCost"), new GUIContent("消耗能量"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldown"), new GUIContent("冷却"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxUsePerBattle"), new GUIContent("最大次数"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("targetType"), new GUIContent("目标类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("category"), new GUIContent("链类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("baseScore"), new GUIContent("基础分"));

            // 预设选择区域（下拉列表 + 应用按钮）
            DrawPresetUI(entry);

            // 删除按钮（带确认对话框）
            if (GUILayout.Button("删除此条目", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("删除条目", "确定删除该条目吗？", "删除", "取消"))
                {
                    entries.DeleteArrayElementAtIndex(index);
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制预设 UI：下拉列表 + 应用按钮
    /// </summary>
    /// <param name="entry">当前条目的 SerializedProperty</param>
    private void DrawPresetUI(SerializedProperty entry)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("预设", GUILayout.Width(40));

        // 预设名称数组，顺序必须与 PresetType 枚举保持一致
        string[] presetNames = { "普通攻击", "特殊攻击", "友方增幅", "敌方减益", "空预设1", "空预设2" };
        selectedPresetIndex = EditorGUILayout.Popup(selectedPresetIndex, presetNames);

        // 应用按钮
        if (GUILayout.Button("应用", GUILayout.Width(50)))
        {
            // 应用对应的预设，参数为枚举值（直接用索引转换）
            ApplyPreset(entry, (PresetType)selectedPresetIndex);

            // 刷新 Inspector 显示
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 添加新条目，并设置合理的默认值
    /// </summary>
    /// <param name="entries">entries 列表属性</param>
    private void AddNewEntry(SerializedProperty entries)
    {
        entries.arraySize++;
        var newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);

        // 设置默认值（注意：不设置 chain，避免覆盖用户已有配置）
        newEntry.FindPropertyRelative("energyCost").intValue = 1;
        newEntry.FindPropertyRelative("cooldown").intValue = 1;     // 回合制，默认冷却 1 回合
        newEntry.FindPropertyRelative("maxUsePerBattle").intValue = 0; // 0 表示不限次数
        newEntry.FindPropertyRelative("targetType").enumValueIndex = (int)AITargetType.Hostile;
        newEntry.FindPropertyRelative("category").enumValueIndex = (int)ChainCategory.Attack;
        newEntry.FindPropertyRelative("baseScore").intValue = 10;

        // 清空编辑器标签（新条目无预设名称）
        var editorLabelProp = newEntry.FindPropertyRelative("editorLabel");
        if (editorLabelProp != null) editorLabelProp.stringValue = "";
    }

    /// <summary>
    /// 预设类型枚举（顺序必须与 UI 下拉列表一致）
    /// </summary>
    private enum PresetType
    {
        NormalAttack,   // 普通攻击
        SpecialAttack,  // 特殊攻击（强力）
        AllyBuff,       // 友方增幅
        EnemyDebuff,    // 敌方减益
        Empty1,         // 空预设1（保留）
        Empty2          // 空预设2（保留）
    }

    /// <summary>
    /// 根据预设类型修改条目的各项数值（不修改 chain），并设置编辑器显示名称
    /// </summary>
    /// <param name="entry">目标条目的 SerializedProperty</param>
    /// <param name="preset">预设类型</param>
    private void ApplyPreset(SerializedProperty entry, PresetType preset)
    {
        // 获取需要修改的属性
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

            case PresetType.Empty1:
            case PresetType.Empty2:
                // 空预设：不做任何修改，仅弹出提示
                EditorUtility.DisplayDialog("空预设", "该预设未定义行为，请手动调整各字段。", "确定");
                return; // 直接返回，不修改字段
        }

        // 应用修改到 SerializedObject
        serializedObject.ApplyModifiedProperties();
    }
}