using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ItemPanelManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Selection selection; // Reference to Selection component
    private Inventory inventory;
    private ActionPanelManager actionPanelManager;

    [Header("Item Names")]
    public TextMeshProUGUI item1Name;
    public TextMeshProUGUI item2Name;
    public TextMeshProUGUI item3Name;
    public TextMeshProUGUI item4Name;

    [Header("Item Icons")]
    public Image item1Icon;
    public Image item2Icon;
    public Image item3Icon;
    public Image item4Icon;

    [Header("Item Buttons")]
    public Button item1Button;
    public Button item2Button;
    public Button item3Button;
    public Button item4Button;

    [Header("Item Quantities (Optional)")]
    public TextMeshProUGUI item1Quantity;
    public TextMeshProUGUI item2Quantity;
    public TextMeshProUGUI item3Quantity;
    public TextMeshProUGUI item4Quantity;

    [Header("Item Usage Settings")]
    [Range(0f, 1f)]
    [Tooltip("Brightness value (V) for items after use. 0 = black, 1 = full brightness")]
    public float itemUsedBrightness = 0.5f;

    // Arrays for easier iteration
    private TextMeshProUGUI[] itemNames;
    private Image[] itemIcons;
    private Button[] itemButtons;
    private TextMeshProUGUI[] itemQuantities;
    
    // Store original colors for restoration
    private Color[] originalIconColors = new Color[4];
    private Color[] originalButtonColors = new Color[4];
    private bool[] originalColorsStored = new bool[4];
    
    // Track current unit to reset item usage on new turn
    private Unit lastDisplayedUnit = null;
    private bool itemUsedThisTurn = false;
    
    // Selection mode state
    private bool isSelectionMode = false;
    private Item currentItem = null;
    private int currentItemIndex = -1;
    private Unit currentCastingUnit = null; // Store the unit using the item for restoring selection
    
    // Button selection mode state (for navigating item buttons)
    private bool isButtonSelectionMode = false;
    private bool ignoreInputThisFrame = false; // Prevents accidental activation when opening panel
    private bool isEnablingButtonSelection = false; // Prevents double-calling EnableButtonSelectionMode
    private int lastSelectedButtonIndex = -1; // Track which button was selected before entering selection mode

    #region Lifecycle Methods
    
    private void Start()
    {
        // Find references
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
        }
        
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        }

        // Initialize arrays
        itemNames = new TextMeshProUGUI[] { item1Name, item2Name, item3Name, item4Name };
        itemIcons = new Image[] { item1Icon, item2Icon, item3Icon, item4Icon };
        itemButtons = new Button[] { item1Button, item2Button, item3Button, item4Button };
        itemQuantities = new TextMeshProUGUI[] { item1Quantity, item2Quantity, item3Quantity, item4Quantity };

        // Subscribe to button clicks
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int itemIndex = i; // Capture index for closure
            if (itemButtons[i] != null)
            {
                itemButtons[i].onClick.AddListener(() => OnItemButtonClicked(itemIndex));
            }
        }
        
        // Subscribe to selection events
        if (selection != null)
        {
            selection.OnSelectionChanged += OnSelectionChanged;
        }

        // Initial update
        UpdateItems();
    }

    /// <summary>
    /// Called when the Items panel becomes visible - refreshes the display and enables button selection
    /// </summary>
    private void OnEnable()
    {
        // Only update if arrays are initialized (Start has been called)
        if (itemNames != null && itemIcons != null && itemButtons != null)
        {
            UpdateItems();
            // Enable button selection mode when panel becomes visible (same as SkillPanelManager)
            EnableButtonSelectionMode();
        }
    }
    
    /// <summary>
    /// Called when the Items panel becomes hidden - disables button selection mode
    /// </summary>
    private void OnDisable()
    {
        DisableButtonSelectionMode();
    }

    private void Update()
    {
        // Check if we should process input
        if (!ShouldProcessInput())
        {
            return;
        }
        
        // Handle button selection mode input
        HandleButtonSelectionModeInput();
        
        // Handle target selection mode input
        HandleTargetSelectionInput();
    }
    
    void LateUpdate()
    {
        // Check for mouse click confirmation after Selection class has processed clicks
        // This ensures we check after Selection's Update has run
        if (isSelectionMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Don't process if clicking on UI elements
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
                return;
            }
            
            // The Selection class's Update() has already handled the mouse click and selected the unit
            // We just need to confirm if there's a valid selection (meaning a unit was clicked)
            if (selection != null && selection.IsValidSelection())
            {
                Unit selectedUnit = selection.GetSelectedUnit();
                // Only confirm if we have a valid unit selected (not empty space)
                if (selectedUnit != null)
                {
                    // Show marker briefly before confirming to give visual feedback
                    StartCoroutine(DelayedConfirmSelection(0.15f));
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from button clicks
        if (itemButtons != null)
        {
            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (itemButtons[i] != null)
                {
                    itemButtons[i].onClick.RemoveAllListeners();
                }
            }
        }
        
        // Unsubscribe from selection events
        if (selection != null)
        {
            selection.OnSelectionChanged -= OnSelectionChanged;
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Updates the item display based on inventory contents
    /// </summary>
    public void UpdateItems()
    {
        // Ensure arrays are initialized
        if (itemNames == null || itemIcons == null || itemButtons == null)
        {
            // Initialize arrays if not already done
            itemNames = new TextMeshProUGUI[] { item1Name, item2Name, item3Name, item4Name };
            itemIcons = new Image[] { item1Icon, item2Icon, item3Icon, item4Icon };
            itemButtons = new Button[] { item1Button, item2Button, item3Button, item4Button };
            itemQuantities = new TextMeshProUGUI[] { item1Quantity, item2Quantity, item3Quantity, item4Quantity };
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("ItemPanelManager: Inventory not found!");
                ClearAllItems();
                return;
            }
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        Unit currentUnit = gameManager != null ? gameManager.GetCurrentUnit() : null;

        // If unit changed, reset item usage flag and restore colors
        if (currentUnit != null && currentUnit != lastDisplayedUnit)
        {
            ResetAllItemColors();
            itemUsedThisTurn = false;
            lastDisplayedUnit = currentUnit;
        }

        List<ItemEntry> items = inventory.Items;

        // Update each item slot (max 4 slots)
        for (int i = 0; i < 4; i++)
        {
            if (i < items.Count && items[i] != null && items[i].item != null)
            {
                PopulateItemSlot(i, items[i], currentUnit);
            }
            else
            {
                ClearItemSlot(i);
            }
        }
        
        // Note: Don't call EnableButtonSelectionMode here - SkillPanelManager does this,
        // but it causes issues with ItemPanelManager because items can become unusable
        // and we don't want to refresh selection every time UpdateItems is called.
        // The selection preservation in EnableButtonSelectionMode should handle cases
        // where it IS called (like from OnEnable).
    }
    
    /// <summary>
    /// Checks if we're currently in target selection mode
    /// </summary>
    public bool IsInSelectionMode()
    {
        return isSelectionMode;
    }
    
    /// <summary>
    /// Cancels selection mode (public method for external cancellation)
    /// </summary>
    public void CancelSelectionModePublic()
    {
        CancelSelectionMode();
    }
    
    /// <summary>
    /// Gets the reason why an item cannot be used (for display purposes)
    /// </summary>
    public string GetItemUnusableReason(Item item, Unit currentUnit)
    {
        return ItemSystem.GetItemUnusableReason(item, currentUnit);
    }
    
    /// <summary>
    /// Enables button selection mode for navigating item buttons vertically
    /// </summary>
    public void EnableButtonSelectionMode()
    {
        // Prevent double-calling
        if (isEnablingButtonSelection)
            return;
            
        isEnablingButtonSelection = true;
        
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
            if (selection == null)
            {
                Debug.LogWarning("ItemPanelManager: Selection component not found. Button selection mode disabled.");
                isEnablingButtonSelection = false;
                return;
            }
        }
        
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
        
        // Store the currently selected button to preserve selection if possible
        Button previouslySelectedButton = GetPreviouslySelectedButton();
        
        // Clear any existing selection first to ensure we start fresh
        selection.ClearSelection();
        
        isButtonSelectionMode = true;
        
        // Get available item buttons (only buttons with items and that are active in hierarchy)
        List<Button> availableButtons = GetAvailableItemButtons();
        
        // Set up selection with available buttons
        if (availableButtons.Count > 0)
        {
            selection.SetSelection(availableButtons.ToArray(), SelectionType.UIButtons);
            
            // Try to restore the previously selected button if it's still in the list
            RestoreButtonSelection(availableButtons, previouslySelectedButton);
            
            // Ignore input for this frame to prevent accidental activation
            ignoreInputThisFrame = true;
        }
        else
        {
            //Debug.LogWarning($"ItemPanelManager: No available item buttons to select! inventory={inventory != null}, items={inventory?.Items != null}, itemsCount={inventory?.Items?.Count ?? 0}");
            // Still set isButtonSelectionMode to false if we can't set up selection
            isButtonSelectionMode = false;
        }
        
        isEnablingButtonSelection = false;
    }
    
    #endregion
    
    #region Target Selection Lifecycle
    
    /// <summary>
    /// Called when an item button is clicked
    /// </summary>
    private void OnItemButtonClicked(int itemIndex)
    {
        if (!CanUseItemNow(itemIndex))
        {
            return;
        }
        
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("ItemPanelManager: Inventory not found!");
                return;
            }
        }
        
        List<ItemEntry> items = inventory.Items;

        if (itemIndex < 0 || itemIndex >= items.Count)
        {
            return;
        }

        ItemEntry entry = items[itemIndex];
        if (entry == null || entry.item == null)
        {
            return;
        }

        Item item = entry.item;
        
        // Only consumables can be used
        if (item.itemType != ItemType.Consumable)
        {
            Debug.LogWarning($"Cannot use item: {item.itemName} (not a consumable)");
            return;
        }
        
        // Check if an item was already used this turn
        if (itemUsedThisTurn)
        {
            Debug.Log("You can only use one item per turn!");
            return;
        }

        // If item targets self, use it immediately
        if (item.targetType == SkillTargetType.Self)
        {
            UseItemOnTarget(item, itemIndex, gameManager.GetCurrentUnit());
            return;
        }

        // Enter selection mode for target selection
        EnterSelectionMode(itemIndex, item);
    }
    
    /// <summary>
    /// Enters selection mode for choosing a target
    /// </summary>
    private void EnterSelectionMode(int itemIndex, Item item)
    {
        if (selection == null)
        {
            Debug.LogWarning("ItemPanelManager: Selection component not found!");
            return;
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("ItemPanelManager: GameManager not found!");
                return;
            }
        }

        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("ItemPanelManager: No current unit!");
            return;
        }

        // Store the currently selected button index before disabling button selection mode
        StoreCurrentButtonSelection();

        // Disable button selection mode before entering target selection mode
        DisableButtonSelectionMode();

        isSelectionMode = true;
        currentItemIndex = itemIndex;
        currentItem = item;

        // Hide user panel when entering selection mode
        if (gameManager != null)
        {
            gameManager.HideUserPanel();
        }

        // Store the current unit for restoring selection later
        currentCastingUnit = currentUnit;

        // Convert SkillTargetType to UnitTargetType
        UnitTargetType targetType = ItemSystem.ConvertTargetType(item.targetType);

        // Setup unit selection (will automatically restore previously selected unit based on target type)
        selection.SetupUnitSelection(targetType, currentUnit, null, item);

        Debug.Log($"Entered selection mode for item: {item.itemName}, Target type: {targetType}");

        // If no valid targets, exit selection mode
        if (selection.Count == 0)
        {
            Debug.LogWarning("No valid targets for this item!");
            CancelSelectionMode();
            return;
        }
        
        // Ignore input for this frame to prevent accidental confirmation
        ignoreInputThisFrame = true;
    }
    
    /// <summary>
    /// Cancels selection mode
    /// </summary>
    private void CancelSelectionMode()
    {
        // Store the currently selected unit before clearing (for next time, based on target type)
        if (selection != null && currentCastingUnit != null)
        {
            selection.StoreLastSelectedUnit(currentCastingUnit);
        }

        isSelectionMode = false;
        currentItem = null;
        currentItemIndex = -1;
        currentCastingUnit = null;
        
        if (selection != null)
        {
            selection.ClearSelection();
        }
        
        // Show user panel again when exiting selection mode
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.ShowUserPanel();
        }
        
        // Store the button index to restore before re-enabling button selection mode
        // (lastSelectedButtonIndex should already be set from EnterSelectionMode, but ensure it's set)
        int buttonIndexToRestore = lastSelectedButtonIndex;
        
        // Re-enable button selection mode to return to item button navigation
        // This will reset selection to index 0, so we need to restore it
        EnableButtonSelectionMode();
        
        // Restore selection to the previously selected button (if it's still available)
        // Use a coroutine to restore after EnableButtonSelectionMode has fully set up the selection
        if (buttonIndexToRestore >= 0)
        {
            StartCoroutine(RestoreButtonSelectionAfterDelay());
        }
        
        Debug.Log("Cancelled item selection mode");
    }
    
    /// <summary>
    /// Confirms the current selection and uses the item
    /// </summary>
    private void ConfirmSelection()
    {
        if (!isSelectionMode || selection == null || currentItem == null)
            return;

        Unit selectedTarget = selection.GetSelectedUnit();
        
        if (selectedTarget != null && currentItemIndex >= 0)
        {
            // Save references before clearing selection mode
            Item item = currentItem;
            int itemIndex = currentItemIndex;
            Unit target = selectedTarget;
            
            // Exit selection mode
            CancelSelectionMode();
            
            // Use the item on the selected target
            UseItemOnTarget(item, itemIndex, target);
        }
    }
    
    /// <summary>
    /// Coroutine to delay confirmation so selection marker is visible
    /// </summary>
    private System.Collections.IEnumerator DelayedConfirmSelection(float delay)
    {
        yield return new WaitForSeconds(delay);
        ConfirmSelection();
    }
    
    #endregion
    
    #region Input Handling
    
    /// <summary>
    /// Checks if we should process input in Update()
    /// </summary>
    private bool ShouldProcessInput()
    {
        // Only process input if this GameObject is active in the hierarchy
        if (!gameObject.activeInHierarchy)
            return false;
        
        // Check if ItemsPanel is actually visible (via ActionPanelManager)
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        }
        if (actionPanelManager != null && actionPanelManager.ItemsPanel != null && !actionPanelManager.ItemsPanel.activeSelf)
        {
            // Items panel is not visible, don't process input
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Handles button selection mode input
    /// </summary>
    private void HandleButtonSelectionModeInput()
    {
        // Handle button selection mode (vertical navigation through item buttons)
        if (isButtonSelectionMode && !isSelectionMode)
        {
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
            }
            else
            {
                // Debug: Check if we're in the right state
                if (selection != null)
                {
                    HandleButtonSelectionInput();
                }
                else
                {
                    Debug.LogWarning("ItemPanelManager: Selection is null in Update()!");
                }
            }
        }
    }
    
    /// <summary>
    /// Handles target selection mode input
    /// </summary>
    private void HandleTargetSelectionInput()
    {
        // Handle selection mode input
        if (isSelectionMode)
        {
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
            }
            else
            {
                HandleTargetSelectionNavigation();
            }
        }
    }
    
    /// <summary>
    /// Handles navigation and confirmation input for target selection
    /// </summary>
    private void HandleTargetSelectionNavigation()
    {
        // Exit with ESC or right-click
        if ((Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame) || 
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
        {
            CancelSelectionMode();
            return;
        }
        
        // Navigate through targets with arrow keys or WASD
        if (Keyboard.current != null && (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || Keyboard.current[Key.A].wasPressedThisFrame))
        {
            if (selection != null)
            {
                selection.Previous();
            }
        }
        else if (Keyboard.current != null && (Keyboard.current[Key.RightArrow].wasPressedThisFrame || Keyboard.current[Key.D].wasPressedThisFrame))
        {
            if (selection != null)
            {
                selection.Next();
            }
        }
        // Confirm selection with Enter or Space
        else if (Keyboard.current != null && (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame))
        {
            // Block input if settings panel is open
            if (InGameMenu.IsSettingsPanelActiveStatic())
            {
                return;
            }
            
            // Block input during skill execution
            if (IsSkillExecuting())
            {
                return;
            }
            
            ConfirmSelection();
        }
        // Mouse click handling is done in LateUpdate() to ensure Selection class processes it first
    }
    
    /// <summary>
    /// Handles input for button selection mode (Up/Down or W/S to cycle, Enter/Space to activate)
    /// </summary>
    private void HandleButtonSelectionInput()
    {
        if (selection == null)
        {
            Debug.LogWarning("ItemPanelManager: Selection is null in HandleButtonSelectionInput!");
            return;
        }
        
        if (!selection.IsValidSelection())
        {
            Debug.LogWarning($"ItemPanelManager: Selection is not valid! Count: {selection.Count}, CurrentIndex: {selection.CurrentIndex}");
            return;
        }
        
        // Cycle with Up/Down arrow keys or W/S (W = previous, S = next)
        if (Keyboard.current != null && (Keyboard.current[Key.UpArrow].wasPressedThisFrame || Keyboard.current[Key.W].wasPressedThisFrame))
        {
            selection.Previous();
        }
        else if (Keyboard.current != null && (Keyboard.current[Key.DownArrow].wasPressedThisFrame || Keyboard.current[Key.S].wasPressedThisFrame))
        {
            selection.Next();
        }
        
        // Activate selected button with Enter or Space
        if (Keyboard.current != null && (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame))
        {
            // Block input if settings panel is open
            if (InGameMenu.IsSettingsPanelActiveStatic())
            {
                return;
            }
            
            // Block input during skill execution
            if (IsSkillExecuting())
            {
                return;
            }
            
            ActivateSelectedButton();
        }
    }
    
    /// <summary>
    /// Activates the currently selected item button
    /// </summary>
    private void ActivateSelectedButton()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        object selectedItem = selection.CurrentSelection;
        if (selectedItem is Button button)
        {
            // Find which item button was selected
            for (int i = 0; i < itemButtons.Length; i++)
            {
                if (itemButtons[i] == button)
                {
                    OnItemButtonClicked(i);
                    break;
                }
            }
        }
    }
    
    #endregion
    
    #region Item Display Management
    
    /// <summary>
    /// Populates an item slot with item data
    /// </summary>
    private void PopulateItemSlot(int index, ItemEntry entry, Unit currentUnit)
    {
        Item item = entry.item;
        int quantity = entry.quantity;

        // Update item name
        if (itemNames != null && index < itemNames.Length && itemNames[index] != null)
        {
            itemNames[index].text = item.itemName;
        }

        // Store original colors if not already stored
        if (!originalColorsStored[index])
        {
            StoreOriginalColors(index);
        }

        // Update item icon
        UpdateItemIcon(index, item);

        // Update quantity display (if available)
        UpdateItemQuantity(index, quantity);

        // Check if item is unusable (healing items when all allies are at full HP)
        bool isUnusable = itemUsedThisTurn || !ItemSystem.CanUseHealingItem(item, currentUnit);
        
        // Update brightness based on item usage or unusability
        UpdateItemBrightness(index, isUnusable);

        // Keep buttons interactable even when unusable (like skills) so pressed state shows on click
        // Visual feedback (dimmed brightness) already indicates unusability
        if (itemButtons != null && index < itemButtons.Length && itemButtons[index] != null)
        {
            itemButtons[index].interactable = true;
        }
    }
    
    /// <summary>
    /// Stores original colors for an item slot
    /// </summary>
    private void StoreOriginalColors(int index)
    {
        // Store original icon color
        if (itemIcons != null && index < itemIcons.Length && itemIcons[index] != null)
        {
            originalIconColors[index] = itemIcons[index].color;
        }
        
        // Store original button color
        if (itemButtons != null && index < itemButtons.Length && itemButtons[index] != null && itemButtons[index].image != null)
        {
            originalButtonColors[index] = itemButtons[index].image.color;
        }
        
        originalColorsStored[index] = true;
    }
    
    /// <summary>
    /// Updates the item icon for a slot
    /// </summary>
    private void UpdateItemIcon(int index, Item item)
    {
        if (itemIcons != null && index < itemIcons.Length && itemIcons[index] != null)
        {
            if (item.icon != null)
            {
                itemIcons[index].sprite = item.icon;
                itemIcons[index].enabled = true;
            }
            else
            {
                itemIcons[index].enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Updates the quantity display for a slot
    /// </summary>
    private void UpdateItemQuantity(int index, int quantity)
    {
        if (itemQuantities != null && index < itemQuantities.Length && itemQuantities[index] != null)
        {
            if (quantity > 1)
            {
                itemQuantities[index].text = quantity.ToString();
            }
            else
            {
                itemQuantities[index].text = "";
            }
        }
    }
    
    /// <summary>
    /// Clears a specific item slot
    /// </summary>
    private void ClearItemSlot(int index)
    {
        if (index < 0 || index >= 4) return;

        if (itemNames != null && index < itemNames.Length && itemNames[index] != null)
        {
            itemNames[index].text = "";
        }

        if (itemIcons != null && index < itemIcons.Length && itemIcons[index] != null)
        {
            itemIcons[index].enabled = false;
        }

        if (itemQuantities != null && index < itemQuantities.Length && itemQuantities[index] != null)
        {
            itemQuantities[index].text = "";
        }

        if (itemButtons != null && index < itemButtons.Length && itemButtons[index] != null)
        {
            itemButtons[index].interactable = false;
        }
        
        // Restore colors when clearing slot
        if (originalColorsStored[index])
        {
            if (itemIcons != null && index < itemIcons.Length && itemIcons[index] != null)
            {
                itemIcons[index].color = originalIconColors[index];
            }
            if (itemButtons != null && index < itemButtons.Length && itemButtons[index] != null && itemButtons[index].image != null)
            {
                itemButtons[index].image.color = originalButtonColors[index];
            }
            originalColorsStored[index] = false;
        }
    }

    /// <summary>
    /// Clears all item displays
    /// </summary>
    private void ClearAllItems()
    {
        for (int i = 0; i < 4; i++)
        {
            ClearItemSlot(i);
        }
    }
    
    /// <summary>
    /// Updates the brightness of an item button/icon based on usage or unusability
    /// </summary>
    private void UpdateItemBrightness(int itemIndex, bool isUnusable)
    {
        if (itemIndex < 0 || itemIndex >= 4)
            return;
        
        // Update icon brightness
        if (itemIcons != null && itemIndex < itemIcons.Length && itemIcons[itemIndex] != null && itemIcons[itemIndex].enabled)
        {
            if (isUnusable)
            {
                // Apply brightness value to icon
                Color originalColor = originalIconColors[itemIndex];
                Color usedColor = new Color(
                    originalColor.r * itemUsedBrightness,
                    originalColor.g * itemUsedBrightness,
                    originalColor.b * itemUsedBrightness,
                    originalColor.a
                );
                itemIcons[itemIndex].color = usedColor;
            }
            else
            {
                // Restore original color
                if (originalColorsStored[itemIndex])
                {
                    itemIcons[itemIndex].color = originalIconColors[itemIndex];
                }
            }
        }
        
        // Update button brightness
        if (itemButtons != null && itemIndex < itemButtons.Length && itemButtons[itemIndex] != null && itemButtons[itemIndex].image != null)
        {
            if (isUnusable)
            {
                // Apply brightness value to button
                Color originalColor = originalButtonColors[itemIndex];
                Color usedColor = new Color(
                    originalColor.r * itemUsedBrightness,
                    originalColor.g * itemUsedBrightness,
                    originalColor.b * itemUsedBrightness,
                    originalColor.a
                );
                itemButtons[itemIndex].image.color = usedColor;
            }
            else
            {
                // Restore original color
                if (originalColorsStored[itemIndex])
                {
                    itemButtons[itemIndex].image.color = originalButtonColors[itemIndex];
                }
            }
        }
    }
    
    /// <summary>
    /// Resets all item colors to their original state when switching units
    /// </summary>
    private void ResetAllItemColors()
    {
        for (int i = 0; i < 4; i++)
        {
            if (originalColorsStored[i])
            {
                if (itemIcons != null && i < itemIcons.Length && itemIcons[i] != null)
                {
                    itemIcons[i].color = originalIconColors[i];
                }
                if (itemButtons != null && i < itemButtons.Length && itemButtons[i] != null && itemButtons[i].image != null)
                {
                    itemButtons[i].image.color = originalButtonColors[i];
                }
            }
        }
    }
    
    #endregion
    
    #region Guard Checks and Conditions
    
    /// <summary>
    /// Checks if an item can be used now (consolidates all guard checks)
    /// </summary>
    private bool CanUseItemNow(int itemIndex)
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                return false;
            }
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        Unit currentUnit = gameManager != null ? gameManager.GetCurrentUnit() : null;

        // Only allow item usage during player unit's turn
        if (currentUnit == null || !currentUnit.IsPlayerUnit || isSelectionMode)
        {
            return false;
        }
        
        // Don't allow item usage during inspect mode
        InspectPanelManager inspectPanel = FindFirstObjectByType<InspectPanelManager>();
        if (inspectPanel != null && inspectPanel.IsInspectMode())
        {
            return false;
        }
        
        // Don't allow item usage during skill execution
        if (actionPanelManager != null && actionPanelManager.IsSkillExecuting())
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if skill execution is in progress
    /// </summary>
    private bool IsSkillExecuting()
    {
        ActionPanelManager actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        return actionPanelManager != null && actionPanelManager.IsSkillExecuting();
    }
    
    #endregion
    
    #region Button Selection Mode Helpers
    
    /// <summary>
    /// Disables button selection mode
    /// </summary>
    private void DisableButtonSelectionMode()
    {
        isButtonSelectionMode = false;
        
        if (selection != null)
        {
            selection.ClearSelection();
        }
    }
    
    /// <summary>
    /// Gets the previously selected button to preserve selection
    /// </summary>
    private Button GetPreviouslySelectedButton()
    {
        Button previouslySelectedButton = null;
        
        // First, try to use lastSelectedButtonIndex (set before entering selection mode)
        // This is the most reliable source when CancelSelectionMode is called
        if (lastSelectedButtonIndex >= 0 && 
            lastSelectedButtonIndex < itemButtons.Length && 
            itemButtons[lastSelectedButtonIndex] != null)
        {
            previouslySelectedButton = itemButtons[lastSelectedButtonIndex];
        }
        // Fallback: check current selection if we're in button selection mode
        else if (isButtonSelectionMode && selection != null && selection.IsValidSelection())
        {
            object currentSelection = selection.CurrentSelection;
            if (currentSelection is Button currentButton)
            {
                // Only preserve if this button is one of our item buttons
                for (int i = 0; i < itemButtons.Length; i++)
                {
                    if (itemButtons[i] == currentButton)
                    {
                        previouslySelectedButton = currentButton;
                        break;
                    }
                }
            }
        }
        
        return previouslySelectedButton;
    }
    
    /// <summary>
    /// Gets available item buttons for selection
    /// </summary>
    private List<Button> GetAvailableItemButtons()
    {
        List<Button> availableButtons = new List<Button>();
        
        if (inventory == null)
        {
            Debug.LogWarning("ItemPanelManager: Inventory is null in EnableButtonSelectionMode!");
        }
        else if (inventory.Items == null)
        {
            Debug.LogWarning("ItemPanelManager: Inventory.Items is null in EnableButtonSelectionMode!");
        }
        else
        {
            for (int i = 0; i < 4 && i < inventory.Items.Count; i++)
            {
                if (inventory.Items[i] != null && inventory.Items[i].item != null && itemButtons[i] != null && itemButtons[i].gameObject.activeInHierarchy)
                {
                    // Include all buttons with items (even if unusable) - same as SkillPanelManager
                    // This prevents selection from jumping when items become unusable
                    availableButtons.Add(itemButtons[i]);
                }
            }
        }
        
        return availableButtons;
    }
    
    /// <summary>
    /// Restores button selection to a previously selected button
    /// </summary>
    private void RestoreButtonSelection(List<Button> availableButtons, Button previouslySelectedButton)
    {
        if (previouslySelectedButton != null && availableButtons.Contains(previouslySelectedButton))
        {
            int targetIndex = availableButtons.IndexOf(previouslySelectedButton);
            if (targetIndex >= 0 && targetIndex < availableButtons.Count)
            {
                // Set the index directly to preserve selection
                selection.SetIndex(targetIndex);
            }
        }
    }
    
    /// <summary>
    /// Stores the current button selection before entering target selection mode
    /// </summary>
    private void StoreCurrentButtonSelection()
    {
        if (selection != null && selection.IsValidSelection())
        {
            object selectedItem = selection.CurrentSelection;
            if (selectedItem is Button button)
            {
                // Find which item button was selected
                for (int i = 0; i < itemButtons.Length; i++)
                {
                    if (itemButtons[i] == button)
                    {
                        lastSelectedButtonIndex = i;
                        break;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Coroutine to restore button selection after EnableButtonSelectionMode has set up the selection
    /// </summary>
    private System.Collections.IEnumerator RestoreButtonSelectionAfterDelay()
    {
        // Wait a frame to ensure EnableButtonSelectionMode has fully set up the selection
        yield return null;
        
        // Only restore if we're still in button selection mode (not if something else changed)
        if (!isButtonSelectionMode || selection == null)
        {
            lastSelectedButtonIndex = -1;
            yield break;
        }
        
        // Restore selection to the previously selected button (if it's still available)
        int buttonIndexToRestore = lastSelectedButtonIndex;
        lastSelectedButtonIndex = -1; // Reset immediately to prevent issues
        
        if (buttonIndexToRestore >= 0 && buttonIndexToRestore < itemButtons.Length && itemButtons[buttonIndexToRestore] != null)
        {
            // Check if the button has an item (is available)
            if (inventory != null && inventory.Items != null && 
                buttonIndexToRestore < inventory.Items.Count && 
                inventory.Items[buttonIndexToRestore] != null && 
                inventory.Items[buttonIndexToRestore].item != null)
            {
                // Button is available, restore selection to it
                if (selection.IsValidSelection())
                {
                    object[] allItems = selection.GetAllItems();
                    if (allItems != null)
                    {
                        for (int i = 0; i < allItems.Length; i++)
                        {
                            if (allItems[i] is Button button && button == itemButtons[buttonIndexToRestore])
                            {
                                selection.SetIndex(i);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Called when selection changes (for visual feedback, etc.)
    /// </summary>
    private void OnSelectionChanged(object selectedItem)
    {
        // Can be used for visual feedback if needed
    }
    
    /// <summary>
    /// Uses an item on a target
    /// </summary>
    private void UseItemOnTarget(Item item, int itemIndex, Unit target)
    {
        if (item == null || target == null || inventory == null)
            return;

        ApplyConsumableResult result = ItemSystem.ApplyConsumableEffect(item, target);

        if (result.healedAmount > 0)
        {
            string targetName = EventLogPanel.GetColoredDisplayNameForUnit(target);
            EventLogPanel.LogEvent($"{targetName} uses {item.itemName} and heals for {result.healedAmount} HP! ({target.CurrentHP}/{target.MaxHP} HP)");
        }

        inventory.RemoveItem(item, 1);
        itemUsedThisTurn = true;
        UpdateItems();

        Debug.Log($"Used {item.itemName} on {target.UnitName}");
    }
    
    #endregion
}
