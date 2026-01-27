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
    
    [Header("Rarity Drop Chances")]
    [Tooltip("Chance out of 100 for each rarity tier to drop")]
    [Range(0, 100)]
    public int commonChance = 50;
    
    [Range(0, 100)]
    public int uncommonChance = 30;
    
    [Range(0, 100)]
    public int rareChance = 15;
    
    [Range(0, 100)]
    public int epicChance = 4;
    
    [Range(0, 100)]
    public int legendaryChance = 1;
    
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

    /// <summary>
    /// Gets a random item from a specific rarity tier AND item type.
    /// Returns null if no matching items are available.
    /// </summary>
    public Item GetRandomItemByRarityAndType(ItemRarity rarity, ItemType itemType)
    {
        List<Item> items = GetItemsByRarity(rarity);
        if (items == null || items.Count == 0)
            return null;

        List<Item> validItems = new List<Item>();
        foreach (var item in items)
        {
            if (item != null && item.itemType == itemType)
                validItems.Add(item);
        }

        if (validItems.Count == 0)
            return null;

        return validItems[Random.Range(0, validItems.Count)];
    }
    
    /// <summary>
    /// Gets a random rarity tier based on the configured drop chances
    /// Uses weighted random selection based on the chance values (out of 100)
    /// Only considers rarities that have items available
    /// </summary>
    /// <returns>A random ItemRarity based on drop chances, or Common if no valid items exist</returns>
    public ItemRarity GetRandomRarity()
    {
        // Build a list of available rarities (ones that have items)
        List<(ItemRarity rarity, int chance)> availableRarities = new List<(ItemRarity, int)>();
        
        if (GetItemsByRarity(ItemRarity.Common).Count > 0)
            availableRarities.Add((ItemRarity.Common, commonChance));
        if (GetItemsByRarity(ItemRarity.Uncommon).Count > 0)
            availableRarities.Add((ItemRarity.Uncommon, uncommonChance));
        if (GetItemsByRarity(ItemRarity.Rare).Count > 0)
            availableRarities.Add((ItemRarity.Rare, rareChance));
        if (GetItemsByRarity(ItemRarity.Epic).Count > 0)
            availableRarities.Add((ItemRarity.Epic, epicChance));
        if (GetItemsByRarity(ItemRarity.Legendary).Count > 0)
            availableRarities.Add((ItemRarity.Legendary, legendaryChance));
        
        // If no rarities have items, return Common as fallback
        if (availableRarities.Count == 0)
        {
            return ItemRarity.Common;
        }
        
        // Calculate total weight from available rarities
        int totalWeight = 0;
        foreach (var (rarity, chance) in availableRarities)
        {
            totalWeight += chance;
        }
        
        // If total weight is 0, return the first available rarity
        if (totalWeight == 0)
        {
            return availableRarities[0].rarity;
        }
        
        // Roll a random number between 0 and totalWeight
        int roll = Random.Range(0, totalWeight);
        
        // Determine which rarity tier the roll falls into
        int currentWeight = 0;
        foreach (var (rarity, chance) in availableRarities)
        {
            currentWeight += chance;
            if (roll < currentWeight)
            {
                return rarity;
            }
        }
        
        // Fallback (shouldn't reach here, but just in case)
        return availableRarities[0].rarity;
    }
    
    /// <summary>
    /// Gets a random item from the loot table based on rarity drop chances
    /// </summary>
    /// <returns>A random item, or null if no items are available</returns>
    public Item GetRandomItem()
    {
        ItemRarity rarity = GetRandomRarity();
        return GetRandomItemByRarity(rarity);
    }
}
