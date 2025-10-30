using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    private SerializedProperty unitTypeProperty;
    private SerializedProperty enemyDataProperty;
    private SerializedProperty creatureDataProperty;
    
    private void OnEnable()
    {
        unitTypeProperty = serializedObject.FindProperty("unitType");
        enemyDataProperty = serializedObject.FindProperty("enemyData");
        creatureDataProperty = serializedObject.FindProperty("creatureUnitData");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Draw unit type dropdown
        EditorGUILayout.PropertyField(unitTypeProperty, new GUIContent("Unit Type"));
        
        EditorGUILayout.Space();
        
        // Show appropriate data field based on unit type
        UnitType currentUnitType = (UnitType)unitTypeProperty.enumValueIndex;
        
        if (currentUnitType == UnitType.Enemy)
        {
            EditorGUILayout.PropertyField(enemyDataProperty, new GUIContent("Enemy Data"));
            
            // Clear creature data if enemy is selected
            if (creatureDataProperty.objectReferenceValue != null)
            {
                creatureDataProperty.objectReferenceValue = null;
            }
        }
        else if (currentUnitType == UnitType.Creature)
        {
            EditorGUILayout.PropertyField(creatureDataProperty, new GUIContent("Creature Data"));
            
            // Clear enemy data if creature is selected
            if (enemyDataProperty.objectReferenceValue != null)
            {
                enemyDataProperty.objectReferenceValue = null;
            }
        }
        
        // Show validation message
        bool hasData = (currentUnitType == UnitType.Enemy && enemyDataProperty.objectReferenceValue != null) ||
                       (currentUnitType == UnitType.Creature && creatureDataProperty.objectReferenceValue != null);
        
        if (!hasData)
        {
            EditorGUILayout.HelpBox("Please assign the appropriate data asset for the selected unit type.", MessageType.Warning);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
