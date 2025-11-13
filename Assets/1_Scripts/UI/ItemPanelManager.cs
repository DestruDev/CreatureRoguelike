using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ItemPanelManager : MonoBehaviour
{
    [Header("References")]
    private Inventory inventory;
    private GameManager gameManager;
    private Selection selection;
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
    
    // Button selection mode state (for navigating item buttons)
    private bool isButtonSelectionMode = false;
    private bool ignoreInputThisFrame = false; // Prevents accidental activation when opening panel

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
    
    /// <summary>
    /// Enables button selection mode for navigating item buttons vertically
    /// </summary>
    public void EnableButtonSelectionMode()
    {
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
            if (selection == null)
            {
                Debug.LogWarning("ItemPanelManager: Selection component not found. Button selection mode disabled.");
                return;
            }
        }
        
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
        
        isButtonSelectionMode = true;
        
        // Get available item buttons (only buttons with items)
        List<Button> availableButtons = new List<Button>();
        if (inventory != null && inventory.Items != null)
        {
            for (int i = 0; i < 4 && i < inventory.Items.Count; i++)
            {
                if (inventory.Items[i] != null && inventory.Items[i].item != null && itemButtons[i] != null)
                {
                    availableButtons.Add(itemButtons[i]);
                }
            }
        }
        
        // Set up selection with available buttons
        if (availableButtons.Count > 0)
        {
            selection.SetSelection(availableButtons.ToArray(), SelectionType.UIButtons);
            // Ignore input for this frame to prevent accidental activation
            ignoreInputThisFrame = true;
        }
    }
    
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
    /// Handles input for button selection mode (Up/Down or W/S to cycle, Enter/Space to activate)
    /// </summary>
    private void HandleButtonSelectionInput()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        // Cycle with Up/Down arrow keys or W/S (W = previous, S = next)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selection.Previous();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selection.Next();
        }
        
        // Activate selected button with Enter or Space
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
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

    private void Update()
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
                HandleButtonSelectionInput();
            }
        }
        
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
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    CancelSelectionMode();
                }
                // Mouse click handling is done in LateUpdate() to ensure Selection class processes it first
                // Navigate through targets with arrow keys or WASD
                else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    if (selection != null)
                    {
                        selection.Previous();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    if (selection != null)
                    {
                        selection.Next();
                    }
                }
                // Confirm selection with Enter or Space
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                {
                    ConfirmSelection();
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Check for mouse click confirmation after Selection class has processed clicks
        // This ensures we check after Selection's Update has run
        if (isSelectionMode && Input.GetMouseButtonDown(0))
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
                Item item = items[i].item;
                int quantity = items[i].quantity;

                // Update item name
                if (itemNames != null && i < itemNames.Length && itemNames[i] != null)
                {
                    itemNames[i].text = item.itemName;
                }

                // Store original colors if not already stored
                if (!originalColorsStored[i])
                {
                    // Store original icon color
                    if (itemIcons != null && i < itemIcons.Length && itemIcons[i] != null)
                    {
                        originalIconColors[i] = itemIcons[i].color;
                    }
                    
                    // Store original button color
                    if (itemButtons != null && i < itemButtons.Length && itemButtons[i] != null && itemButtons[i].image != null)
                    {
                        originalButtonColors[i] = itemButtons[i].image.color;
                    }
                    
                    originalColorsStored[i] = true;
                }

                // Update item icon
                if (itemIcons != null && i < itemIcons.Length && itemIcons[i] != null)
                {
                    if (item.icon != null)
                    {
                        itemIcons[i].sprite = item.icon;
                        itemIcons[i].enabled = true;
                    }
                    else
                    {
                        itemIcons[i].enabled = false;
                    }
                }

                // Update quantity display (if available)
                if (itemQuantities != null && i < itemQuantities.Length && itemQuantities[i] != null)
                {
                    if (quantity > 1)
                    {
                        itemQuantities[i].text = quantity.ToString();
                    }
                    else
                    {
                        itemQuantities[i].text = "";
                    }
                }

                // Update brightness based on item usage
                UpdateItemBrightness(i, itemUsedThisTurn);

                // Enable/disable button based on item usage
                if (itemButtons != null && i < itemButtons.Length && itemButtons[i] != null)
                {
                    itemButtons[i].interactable = !itemUsedThisTurn;
                }
            }
            else
            {
                // Clear empty slot
                ClearItemSlot(i);
            }
        }
        
        // Refresh button selection if in button selection mode (in case available buttons changed)
        if (isButtonSelectionMode)
        {
            EnableButtonSelectionMode();
        }
    }
    
    /// <summary>
    /// Updates the brightness of an item button/icon based on usage
    /// </summary>
    private void UpdateItemBrightness(int itemIndex, bool isUsed)
    {
        if (itemIndex < 0 || itemIndex >= 4)
            return;
        
        // Update icon brightness
        if (itemIcons != null && itemIndex < itemIcons.Length && itemIcons[itemIndex] != null && itemIcons[itemIndex].enabled)
        {
            if (isUsed)
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
            if (isUsed)
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
    /// Called when an item button is clicked
    /// </summary>
    private void OnItemButtonClicked(int itemIndex)
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("ItemPanelManager: Inventory not found!");
                return;
            }
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
        }

        Unit currentUnit = gameManager != null ? gameManager.GetCurrentUnit() : null;

        // Only allow item usage during player unit's turn
        if (currentUnit == null || !currentUnit.IsPlayerUnit || isSelectionMode)
        {
            return;
        }
        
        // Don't allow item usage during inspect mode
        InspectPanelManager inspectPanel = FindFirstObjectByType<InspectPanelManager>();
        if (inspectPanel != null && inspectPanel.IsInspectMode())
        {
            return;
        }
        
        // Don't allow item usage during skill execution
        if (actionPanelManager != null && actionPanelManager.IsSkillExecuting())
        {
            return;
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
            UseItemOnTarget(item, itemIndex, currentUnit);
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

        // Convert SkillTargetType to UnitTargetType
        UnitTargetType targetType = ConvertTargetType(item.targetType);

        // Setup unit selection
        selection.SetupUnitSelection(targetType, currentUnit);

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
    /// Converts SkillTargetType to UnitTargetType
    /// </summary>
    private UnitTargetType ConvertTargetType(SkillTargetType skillTargetType)
    {
        switch (skillTargetType)
        {
            case SkillTargetType.Self:
                return UnitTargetType.Self;
            case SkillTargetType.Ally:
                return UnitTargetType.AllAllies; // Use AllAllies to include self
            case SkillTargetType.Enemy:
                return UnitTargetType.AllEnemies;
            case SkillTargetType.Any:
                return UnitTargetType.Any;
            default:
                return UnitTargetType.Any;
        }
    }
    
    /// <summary>
    /// Called when selection changes (for visual feedback, etc.)
    /// </summary>
    private void OnSelectionChanged(object selectedItem)
    {
        // Can be used for visual feedback if needed
    }
    
    /// <summary>
    /// Handles mouse click on a unit during selection mode
    /// </summary>
    private void HandleMouseClickOnUnit()
    {
        if (!isSelectionMode || selection == null)
            return;

        // Don't process if clicking on UI elements
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // The Selection class's Update() has already handled the mouse click and selected the unit
        // We just need to confirm if there's a valid selection (meaning a unit was clicked)
        if (selection.IsValidSelection())
        {
            Unit selectedUnit = selection.GetSelectedUnit();
            // Only confirm if we have a valid unit selected (not empty space)
            if (selectedUnit != null)
            {
                ConfirmSelection();
            }
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
    /// Cancels selection mode
    /// </summary>
    private void CancelSelectionMode()
    {
        isSelectionMode = false;
        currentItem = null;
        currentItemIndex = -1;
        
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
        
        // Re-enable button selection mode to return to item button navigation
        EnableButtonSelectionMode();
        
        Debug.Log("Cancelled item selection mode");
    }
    
    /// <summary>
    /// Uses an item on a target
    /// </summary>
    private void UseItemOnTarget(Item item, int itemIndex, Unit target)
    {
        if (item == null || target == null || inventory == null)
        {
            return;
        }
        
        // Apply item effects based on subtype
        if (item.itemType == ItemType.Consumable)
        {
            switch (item.consumableSubtype)
            {
                case ConsumableSubtype.Heal:
                    if (target.IsAlive() && item.healAmount > 0)
                    {
                        int oldHP = target.CurrentHP;
                        target.Heal(item.healAmount);
                        int healedAmount = target.CurrentHP - oldHP;
                        
                        // Log to event panel (Unit.Heal already logs, but we want to show item usage)
                        string targetName = EventLogPanel.GetDisplayNameForUnit(target);
                        EventLogPanel.LogEvent($"{targetName} uses {item.itemName} and heals for {healedAmount} HP! ({target.CurrentHP}/{target.MaxHP} HP)");
                    }
                    break;
                    
                case ConsumableSubtype.Damage:
                    // TODO: Implement damage consumables
                    Debug.Log($"Damage consumable not yet implemented: {item.itemName}");
                    break;
                    
                case ConsumableSubtype.Status:
                    // TODO: Implement status consumables
                    Debug.Log($"Status consumable not yet implemented: {item.itemName}");
                    break;
            }
        }
        
        // Remove item from inventory
        inventory.RemoveItem(item, 1);
        
        // Mark item as used this turn
        itemUsedThisTurn = true;
        
        // Update item display (will dim all items)
        UpdateItems();
        
        Debug.Log($"Used {item.itemName} on {target.UnitName}");
    }
}
