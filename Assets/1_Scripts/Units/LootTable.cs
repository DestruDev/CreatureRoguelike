using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "New Loot Table", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("Common Items")]
    [Tooltip("Items that can drop with Common rarity")]
    public List<Item> commonItems = new List<Item>();
    
    [Header("Uncommon Items")]
    [Tooltip("Items that can drop with Uncommon rarity")]
    public List<Item> uncommonItems = new List<Item>();
    
    [Header("Rare Items")]
    [Tooltip("Items that can drop with Rare rarity")]
    public List<Item> rareItems = new List<Item>();
    
    [Header("Epic Items")]
    [Tooltip("Items that can drop with Epic rarity")]
    public List<Item> epicItems = new List<Item>();
    
    [Header("Legendary Items")]
    [Tooltip("Items that can drop with Legendary rarity")]
    public List<Item> legendaryItems = new List<Item>();
    
    [Header("Gold Reward")]
    [Tooltip("Base gold amount awarded after completing each floor")]
    public int goldPerWin = 0;
    
    [Tooltip("Gold multiplier per floor (gold = goldPerWin + floorNumber * floorMultiplier)")]
    public int floorMultiplier = 0;
    
    /// <summary>
    /// Calculates the gold reward for completing a floor
    /// Gold = goldPerWin + (floorNumber * floorMultiplier)
    /// </summary>
    /// <param name="floorNumber">The current floor/stage number</param>
    /// <returns>The total gold amount to award</returns>
    public int CalculateGoldReward(int floorNumber)
    {
        return goldPerWin + (floorNumber * floorMultiplier);
    }
    
    /// <summary>
    /// Gets all items of a specific rarity tier
    /// </summary>
    public List<Item> GetItemsByRarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return commonItems;
            case ItemRarity.Uncommon:
                return uncommonItems;
            case ItemRarity.Rare:
                return rareItems;
            case ItemRarity.Epic:
                return epicItems;
            case ItemRarity.Legendary:
                return legendaryItems;
            default:
                return new List<Item>();
        }
    }
    
    /// <summary>
    /// Gets a random item from a specific rarity tier
    /// Returns null if no items are available for that rarity
    /// </summary>
    public Item GetRandomItemByRarity(ItemRarity rarity)
    {
        List<Item> items = GetItemsByRarity(rarity);
        
        if (items == null || items.Count == 0)
            return null;
        
        // Filter out null entries
        List<Item> validItems = new List<Item>();
        foreach (var item in items)
        {
            if (item != null)
                validItems.Add(item);
        }
        
        if (validItems.Count == 0)
            return null;
        
        return validItems[Random.Range(0, validItems.Count)];
    }
}
