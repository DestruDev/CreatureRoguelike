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
    
    [Header("Item Type")]
    public ItemType itemType = ItemType.Consumable;
    
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
