using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AIDeck))]
public class AIDeckEditor : Editor
{
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
            DrawEntry(entry, i);
        }

        if (GUILayout.Button("+ 添加条目")) { entries.arraySize++; }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntry(SerializedProperty entry, int index)
    {
        EditorGUILayout.BeginVertical("box");
        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, $"条目 {index}", true);

        if (entry.isExpanded)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("chain"), true);
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("energyCost"), new GUIContent("消耗能量"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldown"), new GUIContent("冷却"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("maxUsePerBattle"), new GUIContent("最大次数"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("targetType"), new GUIContent("目标类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("category"), new GUIContent("链类型"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("baseScore"), new GUIContent("基础分"));

            // 预设按钮
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("预设", GUILayout.Width(40));
            if (GUILayout.Button("近战")) ApplyPreset(entry, 0);
            if (GUILayout.Button("远程")) ApplyPreset(entry, 1);
            if (GUILayout.Button("治疗")) ApplyPreset(entry, 2);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("删除此条目", GUILayout.Width(100)))
            {
                var entries = serializedObject.FindProperty("entries");
                if (index >= 0 && index < entries.arraySize)
                    entries.DeleteArrayElementAtIndex(index);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyPreset(SerializedProperty entry, int preset)
    {
        var tt = entry.FindPropertyRelative("targetType");
        var cat = entry.FindPropertyRelative("category");
        tt.enumValueIndex = preset switch { 2 => 1, _ => 0 };
        cat.enumValueIndex = preset switch { 2 => 1, _ => 0 };
        serializedObject.ApplyModifiedProperties();
    }
}
