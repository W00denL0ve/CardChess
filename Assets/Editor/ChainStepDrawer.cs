using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

[CustomPropertyDrawer(typeof(ChainStep), true)]
public class ChainStepDrawer : PropertyDrawer
{
    static readonly string[] typeNames;
    static readonly Type[] types;

    static ChainStepDrawer()
    {
        var baseType = typeof(ChainStep);
        var subTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType))
            .ToArray();
        typeNames = subTypes.Select(t => t.Name).ToArray();
        types = subTypes;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.managedReferenceValue == null)
            return EditorGUIUtility.singleLineHeight + 2;
        float h = EditorGUIUtility.singleLineHeight + 2;
        var prop = property.Copy();
        if (prop.NextVisible(true))
            h += EditorGUI.GetPropertyHeight(prop, true) + 2;
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.managedReferenceValue == null)
        {
            var nullRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int chosen = EditorGUI.Popup(nullRect, label.text, 0, typeNames);
            if (chosen >= 0 && chosen < types.Length)
            {
                var instance = Activator.CreateInstance(types[chosen]);
                property.managedReferenceValue = instance;
                property.serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
            return;
        }

        // Type dropdown
        var typeName = property.managedReferenceFullTypename;
        int idx = Array.FindIndex(typeNames, n => typeName.Contains(n));
        if (idx < 0) idx = 0;

        var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        int newIdx = EditorGUI.Popup(dropdownRect, label.text, idx, typeNames);

        if (newIdx != idx)
        {
            var instance = Activator.CreateInstance(types[newIdx]);
            property.managedReferenceValue = instance;
            property.serializedObject.ApplyModifiedProperties();
        }

        // Fields
        var prop = property.Copy();
        if (prop.NextVisible(true))
        {
            var fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2,
                                     position.width, EditorGUI.GetPropertyHeight(prop, true));
            EditorGUI.PropertyField(fieldRect, prop, true);
        }

        EditorGUI.EndProperty();
    }
}
