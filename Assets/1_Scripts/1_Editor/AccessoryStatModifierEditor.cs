#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AccessoryStatModifier))]
public class AccessoryStatModifierEditor : Editor
{
    private SerializedProperty effectTypeProperty;
    private SerializedProperty directionProperty;
    private SerializedProperty valueTypeProperty;
    private SerializedProperty statProperty;
    private SerializedProperty amountProperty;

    private void OnEnable()
    {
        effectTypeProperty = serializedObject.FindProperty("effectType");
        directionProperty = serializedObject.FindProperty("direction");
        valueTypeProperty = serializedObject.FindProperty("valueType");
        statProperty = serializedObject.FindProperty("stat");
        amountProperty = serializedObject.FindProperty("amount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        AccessoryStatModifier effect = (AccessoryStatModifier)target;
        effect.effectType = AccessoryEffectType.StatModifier;

        EditorGUILayout.LabelField("Accessory Effect Type", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.EnumPopup("Effect Type", effect.effectType);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stat Modifier", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(directionProperty, new GUIContent("Direction", "Increase or decrease the stat"));
        EditorGUILayout.PropertyField(valueTypeProperty, new GUIContent("Value Type", "Flat amount or percentage"));
        EditorGUILayout.PropertyField(statProperty, new GUIContent("Stat", "ATK, DEF, SPD, or HP"));
        EditorGUILayout.PropertyField(amountProperty, new GUIContent("Amount", "Flat value (e.g. 5) or percentage as decimal (e.g. 0.1 = 10%)"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
