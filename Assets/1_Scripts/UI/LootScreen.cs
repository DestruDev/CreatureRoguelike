using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the loot screen that appears after defeating all enemies on a floor
/// Can be attached to GameManager or the UI panel GameObject
/// </summary>
public class LootScreen : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The UI panel GameObject to show/hide (if null, will show/hide this GameObject)")]
    public GameObject lootScreenPanel;
    
    [Tooltip("Button that confirms and advances to the map screen")]
    public Button confirmButton;
    
    [Header("Loot Table")]
    [Tooltip("The LootTable ScriptableObject that contains gold reward settings")]
    public LootTable lootTable;
    
    private GameManager gameManager;
    private LevelMap levelMap;
    private Inventory inventory;
    private LevelNavigation levelNavigation;
    
    private void Start()
    {
        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();
        
        // Find LevelMap
        levelMap = FindFirstObjectByType<LevelMap>();
        
        // Find Inventory
        inventory = FindFirstObjectByType<Inventory>();
        
        // Find LevelNavigation
        levelNavigation = FindFirstObjectByType<LevelNavigation>();
        
        // Subscribe to confirm button click
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        // Initially hide the loot screen
        Hide();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from button click to prevent memory leaks
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
        }
    }
    
    /// <summary>
    /// Shows the loot screen and awards gold for completing the floor
    /// </summary>
    public void Show()
    {
        // Award gold for winning the round
        AwardFloorCompletionGold();
        
        GameObject target = lootScreenPanel != null ? lootScreenPanel : gameObject;
        if (target != null)
        {
            // Debug.Log($"LootScreen: Showing loot screen panel: {target.name}");
            target.SetActive(true);
        }
        else
        {
            Debug.LogWarning("LootScreen: Cannot show - both lootScreenPanel and gameObject are null!");
        }
    }
    
    /// <summary>
    /// Awards gold when a round is won (every round win)
    /// Gold is calculated from the LootTable ScriptableObject
    /// </summary>
    private void AwardFloorCompletionGold()
    {
        // Check if loot table is assigned
        if (lootTable == null)
        {
            Debug.LogWarning("LootScreen: Cannot award gold - LootTable is not assigned!");
            return;
        }
        
        // Get floor number
        int floorNumber = 0;
        if (levelNavigation == null)
        {
            levelNavigation = FindFirstObjectByType<LevelNavigation>();
        }
        
        if (levelNavigation != null)
        {
            floorNumber = levelNavigation.GetCurrentStage();
        }
        
        // Calculate gold from loot table
        int totalGold = lootTable.CalculateGoldReward(floorNumber);
        
        if (totalGold > 0)
        {
            // Ensure inventory reference is set
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }
            
            if (inventory != null)
            {
                inventory.AddCurrency(totalGold);
                Debug.Log($"Awarded {totalGold} gold for winning round (base: {lootTable.goldPerWin}, floor {floorNumber} * {lootTable.floorMultiplier} = {floorNumber * lootTable.floorMultiplier}). New total: {inventory.CurrentGold}");
            }
            else
            {
                Debug.LogWarning("LootScreen: Cannot award gold - Inventory component not found!");
            }
        }
    }
    
    /// <summary>
    /// Hides the loot screen
    /// </summary>
    public void Hide()
    {
        GameObject target = lootScreenPanel != null ? lootScreenPanel : gameObject;
        if (target != null)
        {
            target.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when the confirm button is clicked - advances to the map screen
    /// </summary>
    private void OnConfirmClicked()
    {
        // Hide the loot screen
        Hide();
        
        // Show the map panel
        if (levelMap == null)
        {
            levelMap = FindFirstObjectByType<LevelMap>();
        }
        
        if (levelMap != null)
        {
            levelMap.ShowMapPanel();
        }
        else
        {
            Debug.LogWarning("LootScreen: LevelMap not found! Cannot show map panel.");
        }
    }
}
