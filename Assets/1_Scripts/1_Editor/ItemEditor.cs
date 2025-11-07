#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Item))]
public class ItemEditor : Editor
{
    private SerializedProperty itemNameProperty;
    private SerializedProperty itemIDProperty;
    private SerializedProperty itemDescriptionProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty itemTypeProperty;
    private SerializedProperty consumableSubtypeProperty;
    private SerializedProperty healAmountProperty;
    private SerializedProperty targetTypeProperty;
    
    private void OnEnable()
    {
        itemNameProperty = serializedObject.FindProperty("itemName");
        itemIDProperty = serializedObject.FindProperty("itemID");
        itemDescriptionProperty = serializedObject.FindProperty("itemDescription");
        iconProperty = serializedObject.FindProperty("icon");
        itemTypeProperty = serializedObject.FindProperty("itemType");
        consumableSubtypeProperty = serializedObject.FindProperty("consumableSubtype");
        healAmountProperty = serializedObject.FindProperty("healAmount");
        targetTypeProperty = serializedObject.FindProperty("targetType");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        Item item = (Item)target;
        
        // Basic Info Header
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(itemNameProperty);
        EditorGUILayout.PropertyField(itemIDProperty);
        EditorGUILayout.PropertyField(itemDescriptionProperty);
        EditorGUILayout.PropertyField(iconProperty);
        
        EditorGUILayout.Space();
        
        // Item Type Header
        EditorGUILayout.LabelField("Item Type", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(itemTypeProperty);
        
        // Only show consumable subtype when itemType is Consumable
        if (item.itemType == ItemType.Consumable)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Consumable Subtype", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(consumableSubtypeProperty);
            
            // Only show heal amount when subtype is Heal
            if (item.consumableSubtype == ConsumableSubtype.Heal)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Item Effects", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(healAmountProperty);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
            
            // Target type is auto-set for Heal and Damage, but configurable for Status
            bool isAutoSet = item.consumableSubtype == ConsumableSubtype.Heal || 
                           item.consumableSubtype == ConsumableSubtype.Damage;
            
            EditorGUI.BeginDisabledGroup(isAutoSet);
            EditorGUILayout.PropertyField(targetTypeProperty);
            if (isAutoSet)
            {
                EditorGUILayout.HelpBox(
                    item.consumableSubtype == ConsumableSubtype.Heal 
                        ? "Target type is automatically set to Ally for Heal items." 
                        : "Target type is automatically set to Enemy for Damage items.",
                    MessageType.Info);
            }
            EditorGUI.EndDisabledGroup();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

