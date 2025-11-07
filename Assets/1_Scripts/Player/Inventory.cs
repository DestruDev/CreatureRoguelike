using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the player's inventory including creatures, items, and equipment
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Creatures")]
    [Tooltip("List of creatures the player currently owns")]
    [SerializeField] private List<CreatureUnitData> creatures = new List<CreatureUnitData>();
    
    [Header("Items")]
    [Tooltip("Dictionary-like structure for items and their quantities")]
    [SerializeField] private List<ItemEntry> items = new List<ItemEntry>();
    
    [Tooltip("Maximum number of different item types allowed in inventory")]
    private const int MAX_ITEM_TYPES = 4;
    
    [Header("Equipment")]
    [Tooltip("Equipment slots (for future implementation)")]
    [SerializeField] private EquipmentSlots equipment = new EquipmentSlots();
    
    // Properties
    public int CreatureCount => creatures.Count;
    public List<CreatureUnitData> Creatures => new List<CreatureUnitData>(creatures);
    public List<ItemEntry> Items => new List<ItemEntry>(items);
    public EquipmentSlots Equipment => equipment;
    
    #region Creature Management
    
    /// <summary>
    /// Adds a creature to the inventory
    /// </summary>
    public void AddCreature(CreatureUnitData creature)
    {
        if (creature != null && !creatures.Contains(creature))
        {
            creatures.Add(creature);
            Debug.Log($"Added creature: {creature.unitName}");
        }
    }
    
    /// <summary>
    /// Removes a creature from the inventory
    /// </summary>
    public bool RemoveCreature(CreatureUnitData creature)
    {
        if (creatures.Remove(creature))
        {
            Debug.Log($"Removed creature: {creature.unitName}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Checks if the player has a specific creature
    /// </summary>
    public bool HasCreature(CreatureUnitData creature)
    {
        return creatures.Contains(creature);
    }
    
    /// <summary>
    /// Gets a creature by its ID
    /// </summary>
    public CreatureUnitData GetCreatureByID(string unitID)
    {
        return creatures.FirstOrDefault(c => c != null && c.unitID == unitID);
    }
    
    #endregion
    
    #region Item Management
    
    /// <summary>
    /// Adds an item to the inventory (or increases quantity if already exists)
    /// </summary>
    public void AddItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;
        
        ItemEntry existingEntry = items.FirstOrDefault(entry => entry.item == item);
        
        if (existingEntry != null)
        {
            // Item already exists, just increase quantity
            existingEntry.quantity += quantity;
            Debug.Log($"Added {quantity}x {item.itemName} to inventory (Total: {existingEntry.quantity})");
        }
        else
        {
            // New item type - check if we have room
            if (items.Count >= MAX_ITEM_TYPES)
            {
                Debug.LogWarning($"Cannot add {item.itemName}: Inventory is full (max {MAX_ITEM_TYPES} different item types)");
                return;
            }
            
            items.Add(new ItemEntry { item = item, quantity = quantity });
            Debug.Log($"Added {quantity}x {item.itemName} to inventory");
        }
    }
    
    /// <summary>
    /// Removes an item from the inventory (or decreases quantity)
    /// </summary>
    public bool RemoveItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;
        
        ItemEntry existingEntry = items.FirstOrDefault(entry => entry.item == item);
        
        if (existingEntry != null)
        {
            if (existingEntry.quantity >= quantity)
            {
                existingEntry.quantity -= quantity;
                
                // Remove entry if quantity reaches zero
                if (existingEntry.quantity <= 0)
                {
                    items.Remove(existingEntry);
                }
                
                Debug.Log($"Removed {quantity}x {item.itemName} from inventory");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the quantity of a specific item
    /// </summary>
    public int GetItemQuantity(Item item)
    {
        if (item == null) return 0;
        
        ItemEntry entry = items.FirstOrDefault(e => e.item == item);
        return entry != null ? entry.quantity : 0;
    }
    
    /// <summary>
    /// Checks if the player has a specific item
    /// </summary>
    public bool HasItem(Item item, int minQuantity = 1)
    {
        return GetItemQuantity(item) >= minQuantity;
    }
    
    /// <summary>
    /// Gets an item by its ID
    /// </summary>
    public Item GetItemByID(string itemID)
    {
        ItemEntry entry = items.FirstOrDefault(e => e.item != null && e.item.itemID == itemID);
        return entry != null ? entry.item : null;
    }
    
    /// <summary>
    /// Gets all items of a specific type
    /// </summary>
    public List<ItemEntry> GetItemsByType(ItemType type)
    {
        return items.Where(entry => entry.item != null && entry.item.itemType == type).ToList();
    }
    
    #endregion
    
    #region Equipment Management (Future Implementation)
    
    /// <summary>
    /// Equips an item (for future implementation)
    /// </summary>
    public bool EquipItem(Item item)
    {
        if (item == null || item.itemType != ItemType.Equipment)
        {
            Debug.LogWarning("Cannot equip: Item is null or not equipment type");
            return false;
        }
        
        // TODO: Implement equipment logic
        Debug.Log($"Equipment system not yet implemented. Would equip: {item.itemName}");
        return false;
    }
    
    /// <summary>
    /// Unequips an item (for future implementation)
    /// </summary>
    public bool UnequipItem(Item item)
    {
        if (item == null)
        {
            return false;
        }
        
        // TODO: Implement unequip logic
        Debug.Log($"Equipment system not yet implemented. Would unequip: {item.itemName}");
        return false;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Clears all items from inventory
    /// </summary>
    public void ClearItems()
    {
        items.Clear();
        Debug.Log("Inventory items cleared");
    }
    
    /// <summary>
    /// Clears all creatures from inventory
    /// </summary>
    public void ClearCreatures()
    {
        creatures.Clear();
        Debug.Log("Creatures cleared");
    }
    
    /// <summary>
    /// Gets a summary of the inventory
    /// </summary>
    public string GetInventorySummary()
    {
        System.Text.StringBuilder summary = new System.Text.StringBuilder();
        summary.AppendLine($"Creatures: {CreatureCount}");
        summary.AppendLine($"Items: {items.Count} different types");
        summary.AppendLine($"Total item quantity: {items.Sum(e => e.quantity)}");
        return summary.ToString();
    }
    
    #endregion
}

/// <summary>
/// Represents an item entry with quantity
/// </summary>
[System.Serializable]
public class ItemEntry
{
    public Item item;
    public int quantity = 1;
}

/// <summary>
/// Equipment slots structure (for future implementation)
/// </summary>
[System.Serializable]
public class EquipmentSlots
{
    // TODO: Add equipment slot fields when equipment system is implemented
    // Example:
    // public Item weapon;
    // public Item armor;
    // public Item accessory;
    
    public EquipmentSlots()
    {
        // Initialize equipment slots
    }
}
