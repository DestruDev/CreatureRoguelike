using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionPanelManager : MonoBehaviour
{
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

        // Set initial state - only ActionPanel visible
        ShowActionPanel();

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
        // Don't process if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // Check for ESC key press or right-click to return to ActionPanel
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X))
        {
            GoBackToActionPanel();
        }
        
        // Check for E key to end turn (only when ActionPanel is active)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (ActionPanel != null && ActionPanel.activeSelf)
            {
                EndTurn();
            }
        }
        
        // Handle button selection mode input
        if (isButtonSelectionMode && ActionPanel != null && ActionPanel.activeSelf)
        {
            HandleButtonSelectionInput();
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();

        // Hide ActionPanel during enemy turns
        UpdatePanelVisibility();
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
    /// Updates panel visibility based on whose turn it is
    /// </summary>
    public void UpdatePanelVisibility()
    {
        // Don't update visibility during skill execution or inspect mode
        if (isSkillExecuting || IsInInspectMode())
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
            lastUnit = currentUnit;
        }
        // If it's a player unit's turn
        else if (currentUnit != null && currentUnit.IsPlayerUnit)
        {
            // Show user panel root during player unit turns
            gameManager.ShowUserPanel();
            
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
        }
        else
        {
            // No current unit, reset tracking
            lastUnit = null;
        }
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

    public void ShowActionPanel()
    {
        // Don't allow if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // Hide all panels first
        HideAllPanels();

        // Show ActionPanel
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(true);
        }
        
        // Enable button selection mode
        EnableButtonSelectionMode();
        
        // Update back button visibility
        UpdateBackButtonVisibility();
    }

    public void ShowSkillsPanel()
    {
        // Don't allow if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // If SkillsPanel is already shown, don't do anything (prevent hiding it)
        if (SkillsPanel != null && SkillsPanel.activeSelf)
        {
            return;
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
        // Don't allow if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // If ItemsPanel is already shown, don't do anything (prevent hiding it)
        if (ItemsPanel != null && ItemsPanel.activeSelf)
        {
            return;
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
    
    /// <summary>
    /// Shows the EndTurnButton (used during creature turns)
    /// </summary>
    private void ShowEndTurnButton()
    {
        if (EndTurnButton != null)
        {
            EndTurnButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Ends the current turn and advances to the next unit's turn
    /// </summary>
    public void EndTurn()
    {
        // Don't allow if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // Don't allow if ActionPanel is not active (animations may be playing)
        if (ActionPanel == null || !ActionPanel.activeSelf)
        {
            return;
        }
        
        // Don't allow if skill execution is in progress
        if (IsSkillExecuting())
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
    }
    
    /// <summary>
    /// Gets whether skill execution is currently in progress
    /// </summary>
    public bool IsSkillExecuting()
    {
        return isSkillExecuting;
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
                    UnitNameText.text = currentUnit.UnitName;
                }
            }
        }
    }
    
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
        
        // Set up selection with the two buttons
        Button[] buttons = new Button[] { SkillsButton, ItemsButton };
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
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selection.Previous();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
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
}
