using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    private SerializedProperty creatureDataProperty;
    
    private void OnEnable()
    {
        creatureDataProperty = serializedObject.FindProperty("creatureUnitData");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.HelpBox("Both player units and enemy units use CreatureUnitData. Team assignment is determined by spawn area.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Show CreatureUnitData field
        EditorGUILayout.PropertyField(creatureDataProperty, new GUIContent("Unit Data (CreatureUnitData)"));
        
        EditorGUILayout.Space();
        
        // Show validation message
        if (creatureDataProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Please assign CreatureUnitData.", MessageType.Warning);
        }
        else
        {
            // Show read-only team info
            Unit unit = (Unit)target;
            EditorGUILayout.LabelField("Team Assignment:", unit.IsPlayerUnit ? "Player Unit" : "Enemy Unit");
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Team assignment can be overridden at runtime based on spawn area.", MessageType.Info);
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
