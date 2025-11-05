using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Item name - automatically set to asset name if left empty or set to default")]
    public string itemName = "New Item";
    public string itemID = "";
    [TextArea(3, 5)]
    public string itemDescription = "Item description";
    public Sprite icon;
    

    public ItemType itemType = ItemType.Consumable;
    

    [Tooltip("Subtype for consumable items")]
    public ConsumableSubtype consumableSubtype = ConsumableSubtype.Heal;
    
    [Header("Targeting")]
    [Tooltip("Target type for consumable items (auto-set based on subtype)")]
    public SkillTargetType targetType = SkillTargetType.Ally;
    
#if UNITY_EDITOR
    // Auto-set itemName to asset name if it's empty or still at default value
    private void OnValidate()
    {
        // Only auto-set if itemName is empty or still at default "New Item"
        if (string.IsNullOrEmpty(itemName) || itemName == "New Item")
        {
            // Get the asset path and extract just the filename without extension
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    itemName = fileName;
                }
            }
        }
        
        // Auto-set target type based on consumable subtype
        if (itemType == ItemType.Consumable)
        {
            switch (consumableSubtype)
            {
                case ConsumableSubtype.Heal:
                    targetType = SkillTargetType.Ally;
                    break;
                case ConsumableSubtype.Damage:
                    targetType = SkillTargetType.Enemy;
                    break;
                case ConsumableSubtype.Status:
                    // Status will be decided later - default to Any for now
                    // Don't auto-set, allow manual configuration
                    break;
            }
        }
    }
#endif
}

public enum ItemType
{
    Consumable,     // Single-use items (potions, food, etc.)
    Equipment,       // Equipment items (weapons, armor, etc.)
    Material,       // Crafting materials
    Quest,          // Quest items
    Key,            // Key items
    Misc            // Miscellaneous items
}

public enum ConsumableSubtype
{
    Heal,           // Healing consumables (potions, etc.)
    Damage,         // Damage consumables (throwing weapons, bombs, etc.)
    Status          // Status effect consumables (buffs, debuffs, etc.)
}
