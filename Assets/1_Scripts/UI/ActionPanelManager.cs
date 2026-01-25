using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ActionPanelManager : MonoBehaviour
{
    // Static flag to indicate ESC was handled by ActionPanelManager (prevents InGameMenu from also handling it)
    public static bool EscHandledThisFrame = false;
    
    [Header("Panels")]
    public GameObject ActionPanel;
    public GameObject SkillsPanel;
    public GameObject ItemsPanel;

    [Header("Buttons")]
    public Button SkillsButton;
    public Button ItemsButton;
    public Button EndTurnButton;
    public Button BackButton;
    
    [Header("Unit Info")]
    public TextMeshProUGUI UnitNameText;
    
    [Header("Inspect Tooltip")]
    [Tooltip("GameObject that should sync with UI hiding/showing")]
    public GameObject inspectTooltip;
    
    [Header("Font Size Settings")]
    [Tooltip("Maximum font size for short unit names")]
    [SerializeField] private float maxFontSize = 36f;
    
    [Tooltip("Minimum font size for very long unit names")]
    [SerializeField] private float minFontSize = 18f;
    
    [Tooltip("Name length at which font size starts scaling down (names this length or shorter use max font size)")]
    [SerializeField] private int shortNameThreshold = 10;
    
    [Tooltip("Name length at which font size reaches minimum (names this length or longer use min font size)")]
    [SerializeField] private int longNameThreshold = 30;

    [Header("References")]
    private GameManager gameManager;
    private TurnOrder turnOrder;
    private InspectPanelManager inspectPanelManager;
    private Selection selection;
    
    // Track the last unit to detect when a new turn starts
    private Unit lastUnit = null;
    
    // Track if we're in skill execution mode (to block UI from showing)
    private bool isSkillExecuting = false;
    
    // Track if panels have been hidden for game over
    private bool panelsHiddenForGameOver = false;
    
    // Track if we're in button selection mode
    private bool isButtonSelectionMode = false;

    private void Awake()
    {
        // Hide all UI elements immediately to prevent them from showing before userPanelRoot is hidden
        // This runs before Start() to ensure they're hidden as early as possible
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(false);
        }
        
        if (EndTurnButton != null)
        {
            EndTurnButton.gameObject.SetActive(false);
        }
        
        if (UnitNameText != null)
        {
            UnitNameText.gameObject.SetActive(false);
        }
        
        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(false);
        }
        
        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();

        // Find TurnOrder
        turnOrder = FindFirstObjectByType<TurnOrder>();
        
        // Find InspectPanelManager
        inspectPanelManager = FindFirstObjectByType<InspectPanelManager>();
        
        // Find Selection
        selection = FindFirstObjectByType<Selection>();

        // Clear unit name text to prevent showing placeholder before initialization
        if (UnitNameText != null)
        {
            UnitNameText.text = "";
        }

        // Don't show ActionPanel on start - UI should be hidden when map is open
        // ActionPanel will be shown when a level starts and a player unit's turn begins
        // ShowActionPanel();

        // Subscribe to button clicks
        if (SkillsButton != null)
        {
            SkillsButton.onClick.AddListener(ShowSkillsPanel);
        }

        if (ItemsButton != null)
        {
            ItemsButton.onClick.AddListener(ShowItemsPanel);
        }

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.AddListener(EndTurn);
        }
        
        if (BackButton != null)
        {
            BackButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        // Subscribe to selection changes
        if (selection != null)
        {
            selection.OnSelectionChanged += OnButtonSelectionChanged;
        }
    }

    private void Update()
    {
        // Reset the static flag at the start of each frame
        EscHandledThisFrame = false;
        
        // Don't process if input is blocked
        if (IsInputBlocked())
        {
            return;
        }
        
        // Handle back input (ESC / right-click / X)
        HandleBackInput();
        
        // Handle end turn input (E key)
        HandleEndTurnInput();
        
        // Handle button selection mode input
        HandleButtonSelectionModeInput();
        
        // Update back button visibility
        UpdateBackButtonVisibility();

        // Hide ActionPanel during enemy turns
        UpdatePanelVisibility();
    }
    

    private void OnDestroy()
    {
        // Unsubscribe from button clicks to prevent memory leaks
        if (SkillsButton != null)
        {
            SkillsButton.onClick.RemoveListener(ShowSkillsPanel);
        }

        if (ItemsButton != null)
        {
            ItemsButton.onClick.RemoveListener(ShowItemsPanel);
        }

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.RemoveListener(EndTurn);
        }
        
        if (BackButton != null)
        {
            BackButton.onClick.RemoveListener(OnBackButtonClicked);
        }
        
        // Unsubscribe from selection changes
        if (selection != null)
        {
            selection.OnSelectionChanged -= OnButtonSelectionChanged;
        }
    }

    #region Public Methods - Panel Management
    
    /// <summary>
    /// Updates panel visibility based on whose turn it is
    /// </summary>
    public void UpdatePanelVisibility()
    {
        // Don't update visibility if blocked
        if (IsInputBlocked())
        {
            return;
        }
        
        // Don't update visibility during selection mode (skill/item/normal attack target selection)
        if (IsInSelectionMode())
        {
            return;
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }
        
        // Check if game is over and hide all panels
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
        }
        
        if (turnOrder != null && turnOrder.IsGameEnded())
        {
            if (!panelsHiddenForGameOver)
            {
                // Hide user panel root which contains all UI elements
                gameManager.HideUserPanel();
                // Hide inspect tooltip when game is over
                if (inspectTooltip != null)
                {
                    inspectTooltip.SetActive(false);
                }
                panelsHiddenForGameOver = true;
            }
            return; // Don't process further if game is over
        }
        else
        {
            // Reset flag if game is not over (in case of restart)
            panelsHiddenForGameOver = false;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();
        
        // Update unit name text
        UpdateUnitNameText(currentUnit);

        // If it's an enemy's turn (based on spawn area assignment), hide user panel root
        if (currentUnit != null && currentUnit.IsEnemyUnit)
        {
            gameManager.HideUserPanel();
            // Hide inspect tooltip during enemy turns
            if (inspectTooltip != null)
            {
                inspectTooltip.SetActive(false);
            }
            lastUnit = currentUnit;
        }
        // If it's a player unit's turn
        else if (currentUnit != null && currentUnit.IsPlayerUnit)
        {
            // Show user panel root during player unit turns
            gameManager.ShowUserPanel();
            
            // Show inspect tooltip during player unit turns
            if (inspectTooltip != null)
            {
                inspectTooltip.SetActive(true);
            }
            
            // If this is a different unit from last frame (new turn started), reset to ActionPanel
            bool isNewTurn = lastUnit != currentUnit;
            if (isNewTurn)
            {
                // Always show ActionPanel at the start of a new creature turn
                ShowActionPanel();
                lastUnit = currentUnit;
            }
            // If no panels are visible and it's not a new turn, show ActionPanel
            else if (ActionPanel != null && !ActionPanel.activeSelf && 
                     (SkillsPanel == null || !SkillsPanel.activeSelf) && 
                     (ItemsPanel == null || !ItemsPanel.activeSelf))
            {
                ShowActionPanel();
            }
            // If ActionPanel is already active, ensure EndTurnButton is also shown
            else if (ActionPanel != null && ActionPanel.activeSelf)
            {
                if (EndTurnButton != null)
                {
                    EndTurnButton.gameObject.SetActive(true);
                    EndTurnButton.interactable = true;
                }
            }
        }
        else
        {
            // No current unit, hide user panel and reset tracking
            gameManager.HideUserPanel();
            // Hide inspect tooltip when no current unit
            if (inspectTooltip != null)
            {
                inspectTooltip.SetActive(false);
            }
            lastUnit = null;
        }
    }

    public void ShowActionPanel()
    {
        // Don't allow if blocked
        if (!CanInteractWithActionUI())
        {
            return;
        }
        
        // Ensure userPanelRoot is shown BEFORE showing ActionPanel and EndTurnButton
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.ShowUserPanel();
        }
        
        // Update unit name BEFORE showing the panel to prevent flickering
        if (gameManager != null)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            UpdateUnitNameText(currentUnit);
        }
        
        // Hide all panels first
        HideAllPanels();

        // Show ActionPanel
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(true);
        }

        // Re-enable end turn button and unit name text when showing the action panel
        if (EndTurnButton != null)
        {
            EndTurnButton.gameObject.SetActive(true);
            EndTurnButton.interactable = true;
        }
        if (UnitNameText != null)
        {
            UnitNameText.gameObject.SetActive(true);
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();
        
        // Force canvas update to ensure UI layout is finalized before enabling button selection
        Canvas.ForceUpdateCanvases();
        
        // Enable button selection mode immediately
        EnableButtonSelectionMode();
        
        // Update marker position after layout has fully settled (prevents marker appearing in wrong position)
        StartCoroutine(DelayedUpdateSelectionMarkerPosition());
    }

    public void ShowSkillsPanel()
    {
        // Don't allow if blocked
        if (!CanInteractWithActionUI())
        {
            return;
        }
        
        // If SkillsPanel is already shown, don't do anything (prevent hiding it)
        if (SkillsPanel != null && SkillsPanel.activeSelf)
        {
            return;
        }
        
        // Ensure userPanelRoot is shown BEFORE showing SkillsPanel
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.ShowUserPanel();
        }
        
        // Disable button selection mode when leaving ActionPanel
        DisableButtonSelectionMode();
        
        // Hide all panels first
        HideAllPanels();

        // Show SkillsPanel
        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(true);
        }
        
        // Enable button selection mode for skill buttons
        SkillPanelManager skillPanelManager = FindFirstObjectByType<SkillPanelManager>();
        if (skillPanelManager != null)
        {
            // Update skills first to ensure buttons are properly set up
            skillPanelManager.UpdateSkills();
            // Then enable button selection
            skillPanelManager.EnableButtonSelectionMode();
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();
    }

    public void ShowItemsPanel()
    {
        // Don't allow if blocked
        if (!CanInteractWithActionUI())
        {
            return;
        }
        
        // If ItemsPanel is already shown, don't do anything (prevent hiding it)
        if (ItemsPanel != null && ItemsPanel.activeSelf)
        {
            return;
        }
        
        // Ensure userPanelRoot is shown BEFORE showing ItemsPanel
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.ShowUserPanel();
        }
        
        // Disable button selection mode when leaving ActionPanel
        DisableButtonSelectionMode();
        
        // Hide all panels first
        HideAllPanels();

        // Show ItemsPanel
        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(true);
        }
        
        // Update item panel display and enable button selection
        ItemPanelManager itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelManager != null)
        {
            itemPanelManager.UpdateItems();
            // EnableButtonSelectionMode will be called by OnEnable, but we call it here too
            // to ensure it's set up even if OnEnable was already called
            itemPanelManager.EnableButtonSelectionMode();
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();
    }

    public void HideAllPanels()
    {
        // Disable button selection mode when hiding panels
        DisableButtonSelectionMode();
        
        // Clear selection to hide selection markers when panels are hidden
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
        }
        
        if (selection != null)
        {
            selection.ClearSelection();
        }
        
        // Hide all panels to ensure only one is visible at a time
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(false);
        }

        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(false);
        }

        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(false);
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();
    }
    
    /// <summary>
    /// Called when the back button is clicked - returns to ActionPanel
    /// </summary>
    private void OnBackButtonClicked()
    {
        GoBackToActionPanel();
    }
    
    /// <summary>
    /// Returns to ActionPanel from SkillsPanel or ItemsPanel (same as ESC/right-click)
    /// </summary>
    private void GoBackToActionPanel()
    {
        // First, cancel any active selection modes (target selection)
        SkillPanelManager skillPanelManager = FindFirstObjectByType<SkillPanelManager>();
        if (skillPanelManager != null && skillPanelManager.IsInSelectionMode())
        {
            skillPanelManager.CancelSelectionModePublic();
            // Don't return to ActionPanel if we're still in SkillsPanel
            if (SkillsPanel != null && SkillsPanel.activeSelf)
            {
                return; // Stay in SkillsPanel, just cancel target selection
            }
        }
        
        ItemPanelManager itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelManager != null && itemPanelManager.IsInSelectionMode())
        {
            itemPanelManager.CancelSelectionModePublic();
            // Don't return to ActionPanel if we're still in ItemsPanel
            if (ItemsPanel != null && ItemsPanel.activeSelf)
            {
                return; // Stay in ItemsPanel, just cancel target selection
            }
        }
        
        // If SkillsPanel or ItemsPanel is active, return to ActionPanel
        if (SkillsPanel != null && SkillsPanel.activeSelf)
        {
            ShowActionPanel();
        }
        else if (ItemsPanel != null && ItemsPanel.activeSelf)
        {
            ShowActionPanel();
        }
    }
    
    /// <summary>
    /// Updates the back button visibility - only show when SkillsPanel or ItemsPanel is active
    /// </summary>
    private void UpdateBackButtonVisibility()
    {
        if (BackButton == null) return;
        
        // Show back button only when SkillsPanel or ItemsPanel is active
        bool shouldShow = (SkillsPanel != null && SkillsPanel.activeSelf) || 
                         (ItemsPanel != null && ItemsPanel.activeSelf);
        
        BackButton.gameObject.SetActive(shouldShow);
    }
    
    #endregion
    
    #region Public Methods - Turn Management
    
    /// <summary>
    /// Ends the current turn and advances to the next unit's turn
    /// </summary>
    public void EndTurn()
    {
        // Don't allow if blocked
        if (!CanInteractWithActionUI())
        {
            return;
        }
        
        // Don't allow if ActionPanel is not active (animations may be playing)
        if (ActionPanel == null || !ActionPanel.activeSelf)
        {
            return;
        }
        
        // Advance to next turn (this will reset the gauge)
        // Note: StartTurn is now called in GameManager.SetCurrentUnit when a player unit's turn begins
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
        }

        if (turnOrder != null)
        {
            turnOrder.AdvanceToNextTurn();
        }
        else
        {
            Debug.LogWarning("ActionPanelManager: Cannot end turn - TurnOrder not found!");
        }
    }
    
    /// <summary>
    /// Hides the EndTurnButton (used during enemy turns)
    /// </summary>
    public void HideEndTurnButton()
    {
        if (EndTurnButton != null)
        {
            EndTurnButton.gameObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public Methods - Skill Execution
    
    /// <summary>
    /// Hides all action UI elements (called during skill execution to prevent player input)
    /// </summary>
    public void HideAllActionUI()
    {
        // Set flag to block UpdatePanelVisibility from showing UI during skill execution
        isSkillExecuting = true;
        
        // Clear selection to hide selection markers during animations
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
        }
        
        if (selection != null)
        {
            selection.ClearSelection();
        }
        
        // Hide user panel root which contains all UI elements
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager != null)
        {
            gameManager.HideUserPanel();
        }
        
        // Hide inspect tooltip
        if (inspectTooltip != null)
        {
            inspectTooltip.SetActive(false);
        }
    }
    
    /// <summary>
    /// Shows action UI elements based on current unit (called after skill execution completes)
    /// </summary>
    public void ShowAllActionUI()
    {
        // Clear the skill execution flag
        isSkillExecuting = false;
        
        // Let UpdatePanelVisibility handle showing the correct UI based on whose turn it is
        // This will be called in the next Update() cycle
        // We can force an update by calling UpdatePanelVisibility directly
        UpdatePanelVisibility();
        
        // Show inspect tooltip if it should be visible (only during player unit turns)
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager != null)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            if (currentUnit != null && currentUnit.IsPlayerUnit && inspectTooltip != null)
            {
                inspectTooltip.SetActive(true);
            }
        }
    }
    
    /// <summary>
    /// Gets whether skill execution is currently in progress
    /// </summary>
    public bool IsSkillExecuting()
    {
        return isSkillExecuting;
    }
    
    /// <summary>
    /// Hides action UI for round end and blocks visibility until next round starts.
    /// </summary>
    public void HideForRoundEnd()
    {
        panelsHiddenForGameOver = true;
        HideAllPanels();

        // Disable unit name and end turn button during round end
        if (UnitNameText != null)
        {
            UnitNameText.gameObject.SetActive(false);
        }
        if (EndTurnButton != null)
        {
            EndTurnButton.interactable = false;
            EndTurnButton.gameObject.SetActive(false);
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.HideUserPanel();
        }
        
        // Hide inspect tooltip during round end
        if (inspectTooltip != null)
        {
            inspectTooltip.SetActive(false);
        }
    }

    /// <summary>
    /// Resets all ActionPanelManager state flags and hides all panels
    /// Called when advancing to next level
    /// </summary>
    public void Reset()
    {
        // Reset all state flags
        isSkillExecuting = false;
        lastUnit = null;
        panelsHiddenForGameOver = false;
        isButtonSelectionMode = false;
        
        // Hide all panels and clear selection
        HideAllPanels();
        
        // Hide inspect tooltip when resetting
        if (inspectTooltip != null)
        {
            inspectTooltip.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public Methods - Button Selection
    
    #endregion
    
    #region Input Handling
    
    /// <summary>
    /// Handles back input (ESC / right-click / X key)
    /// </summary>
    private void HandleBackInput()
    {
        // Check for ESC key press or right-click to return to ActionPanel or cancel selection
        // ESC key: only handle if SkillsPanel or ItemsPanel are active (otherwise let InGameMenu handle it for settings)
        // Right-click and X key: always handle
        bool isEscapeKey = Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame;
        bool isRightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool isXKey = Keyboard.current != null && Keyboard.current[Key.X].wasPressedThisFrame;
        
        if (isEscapeKey || isRightClick || isXKey)
        {
            // For ESC key: only handle if SkillsPanel or ItemsPanel are active
            // But don't handle if settings panel is open (let InGameMenu close it first)
            if (isEscapeKey)
            {
                // If settings panel is open, let InGameMenu handle ESC to close it
                if (InGameMenu.IsSettingsPanelActiveStatic())
                {
                    return; // Let InGameMenu handle ESC to close settings panel
                }
                
                bool skillsPanelActive = SkillsPanel != null && SkillsPanel.activeSelf;
                bool itemsPanelActive = ItemsPanel != null && ItemsPanel.activeSelf;
                
                // Only handle ESC if one of the panels is active
                if (!skillsPanelActive && !itemsPanelActive)
                {
                    // Let InGameMenu handle ESC to open settings panel
                    return;
                }
                
                // Mark that we're handling ESC
                EscHandledThisFrame = true;
            }
            
            // Handle right-click, X key, or ESC when panels are active
            GoBackToActionPanel();
        }
    }
    
    /// <summary>
    /// Handles end turn input (E key)
    /// </summary>
    private void HandleEndTurnInput()
    {
        // Check for E key to end turn (only when ActionPanel is active)
        if (Keyboard.current != null && Keyboard.current[Key.E].wasPressedThisFrame)
        {
            if (ActionPanel != null && ActionPanel.activeSelf)
            {
                EndTurn();
            }
        }
    }
    
    /// <summary>
    /// Handles button selection mode input
    /// </summary>
    private void HandleButtonSelectionModeInput()
    {
        // Handle button selection mode input
        if (isButtonSelectionMode && ActionPanel != null && ActionPanel.activeSelf)
        {
            HandleButtonSelectionInput();
        }
    }
    
    #endregion
    
    #region Guard Checks
    
    /// <summary>
    /// Checks if input should be blocked (inspect mode or skill execution)
    /// </summary>
    private bool IsInputBlocked()
    {
        return IsInInspectMode() || isSkillExecuting;
    }
    
    /// <summary>
    /// Checks if we can interact with action UI (not in inspect mode)
    /// </summary>
    private bool CanInteractWithActionUI()
    {
        return !IsInInspectMode();
    }
    
    /// <summary>
    /// Checks if inspect mode is currently active
    /// </summary>
    private bool IsInInspectMode()
    {
        if (inspectPanelManager == null)
        {
            inspectPanelManager = FindFirstObjectByType<InspectPanelManager>();
        }
        
        return inspectPanelManager != null && inspectPanelManager.IsInspectMode();
    }
    
    /// <summary>
    /// Checks if we're in any selection mode (skill/item)
    /// </summary>
    private bool IsInSelectionMode()
    {
        // Check if skill or item selection mode is active
        SkillPanelManager skillPanelManager = FindFirstObjectByType<SkillPanelManager>();
        if (skillPanelManager != null && skillPanelManager.IsInSelectionMode())
        {
            return true;
        }
        
        ItemPanelManager itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelManager != null && itemPanelManager.IsInSelectionMode())
        {
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region Button Selection Mode
    
    /// <summary>
    /// Enables button selection mode for cycling between SkillsButton and ItemsButton
    /// </summary>
    private void EnableButtonSelectionMode()
    {
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
            if (selection == null)
            {
                Debug.LogWarning("ActionPanelManager: Selection component not found. Button selection mode disabled.");
                return;
            }
        }
        
        isButtonSelectionMode = true;
        
        // Set up selection with the buttons (only include non-null buttons)
        List<Button> buttonList = new List<Button>();
        if (SkillsButton != null) buttonList.Add(SkillsButton);
        if (ItemsButton != null) buttonList.Add(ItemsButton);
        
        Button[] buttons = buttonList.ToArray();
        selection.SetSelection(buttons, SelectionType.UIButtons);
        
        // Update markers based on initial selection
        UpdateButtonSelectionMarkers();
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
            selection.HideAllUISelectionMarkers();
        }
    }
    
    /// <summary>
    /// Handles input for button selection mode (Left/Right arrows or A/D to cycle, Enter/Space to activate)
    /// </summary>
    private void HandleButtonSelectionInput()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        // Cycle with Left/Right arrow keys or A/D (A = previous, D = next)
        // Up/Down and W/S are disabled for horizontal button navigation
        if (Keyboard.current != null && (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || Keyboard.current[Key.A].wasPressedThisFrame))
        {
            selection.Previous();
        }
        else if (Keyboard.current != null && (Keyboard.current[Key.RightArrow].wasPressedThisFrame || Keyboard.current[Key.D].wasPressedThisFrame))
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
    /// Activates the currently selected button
    /// </summary>
    private void ActivateSelectedButton()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        object selectedItem = selection.CurrentSelection;
        if (selectedItem is Button button)
        {
            if (button == SkillsButton)
            {
                ShowSkillsPanel();
            }
            else if (button == ItemsButton)
            {
                ShowItemsPanel();
            }
        }
    }
    
    /// <summary>
    /// Called when button selection changes - updates the visual markers
    /// </summary>
    private void OnButtonSelectionChanged(object selectedItem)
    {
        if (!isButtonSelectionMode || selection == null)
            return;
        
        UpdateButtonSelectionMarkers();
    }
    
    /// <summary>
    /// Updates the visual markers based on current button selection
    /// </summary>
    private void UpdateButtonSelectionMarkers()
    {
        if (selection == null)
            return;
        
        // Hide all markers first
        selection.HideAllUISelectionMarkers();
        
        // Show marker for currently selected button
        if (selection.IsValidSelection())
        {
            object selectedItem = selection.CurrentSelection;
            if (selectedItem is Button button)
            {
                int markerIndex = -1;
                
                // Map button to marker index based on button order
                // SkillsButton = 0, ItemsButton = 1
                if (button == SkillsButton)
                {
                    markerIndex = 0; // SelectMarker1 for SkillsButton
                }
                else if (button == ItemsButton)
                {
                    markerIndex = 1; // SelectMarker2 for ItemsButton
                }
                
                if (markerIndex >= 0)
                {
                    selection.SetUISelectionMarker(markerIndex, true);
                }
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Coroutine to update selection marker position after UI layout has fully settled
    /// </summary>
    private System.Collections.IEnumerator DelayedUpdateSelectionMarkerPosition()
    {
        // Wait one frame to ensure all layout updates are complete
        yield return null;
        
        // Check if ActionPanel is still active (might have been hidden)
        if (ActionPanel == null || !ActionPanel.activeSelf)
        {
            yield break;
        }
        
        // Force canvas update to ensure UI layout is finalized
        Canvas.ForceUpdateCanvases();
        
        // Update the selection marker position by refreshing the selection
        if (selection != null && selection.IsValidSelection())
        {
            // Trigger marker update by setting the index to itself
            int currentIndex = selection.CurrentIndex;
            selection.SetIndex(currentIndex);
        }
    }
    
    /// <summary>
    /// Gets the appropriate attack-to-hurt delay based on caster's animation length
    /// </summary>
    private float GetAttackToHurtDelay(Unit caster, GameManager gameManager)
    {
        if (gameManager == null)
        {
            return 0.5f; // Fallback
        }
        
        if (caster != null)
        {
            UnitAnimations unitAnimations = caster.GetComponent<UnitAnimations>();
            if (unitAnimations != null)
            {
                float animationLength = unitAnimations.GetAttackAnimationLength();
                if (animationLength > 0f)
                {
                    // Return animation length + post-animation delay
                    return animationLength + gameManager.postAttackAnimationDelay;
                }
            }
        }
        // Fallback if animation length can't be determined
        return 0.5f;
    }
    
    /// <summary>
    /// Updates the UnitNameText UI with the current unit's name
    /// </summary>
    private void UpdateUnitNameText(Unit currentUnit)
    {
        if (UnitNameText != null)
        {
            if (currentUnit != null)
            {
                // Update text for creature turns using ScriptableObject name
                // Note: userPanelRoot visibility is handled by UpdatePanelVisibility()
                if (currentUnit.IsPlayerUnit)
                {
                    string displayName = currentUnit.UnitName;
                    UnitNameText.text = displayName;
                    UnitNameText.fontSize = CalculateFontSize(displayName);
                }
                else
                {
                    // Clear text for non-player units (shouldn't be visible anyway)
                    UnitNameText.text = "";
                }
            }
            else
            {
                // Clear text when no current unit
                UnitNameText.text = "";
            }
        }
    }
    
    /// <summary>
    /// Calculates the font size based on the length of the unit name
    /// Shorter names use max font size, longer names scale down to min font size
    /// </summary>
    private float CalculateFontSize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return maxFontSize;
        
        int nameLength = name.Length;
        
        // If name is at or below short threshold, use max font size
        if (nameLength <= shortNameThreshold)
        {
            return maxFontSize;
        }
        
        // If name is at or above long threshold, use min font size
        if (nameLength >= longNameThreshold)
        {
            return minFontSize;
        }
        
        // Interpolate between max and min based on name length
        // Calculate how far along the range we are (0 = at short threshold, 1 = at long threshold)
        float range = longNameThreshold - shortNameThreshold;
        float position = (nameLength - shortNameThreshold) / range;
        
        // Lerp from max to min
        return Mathf.Lerp(maxFontSize, minFontSize, position);
    }
    
    #endregion
}
