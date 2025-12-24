using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class InspectPanelManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject InspectPanel;
    
    [Header("Settings")]
    public bool enableInspectFunctionality = true;
    
    [Header("Inspect Highlight Color")]
    [Tooltip("Color for highlight markers when in inspect mode (applies to all units)")]
    public Color inspectHighlightColor = Color.cyan;
    
    [Tooltip("Transparency/Alpha for inspect highlight markers (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float inspectHighlightAlpha = 1f;
    
    [Header("Inspect Info")]
    public TextMeshProUGUI InspectName;
    public TextMeshProUGUI InspectHP;
    public TextMeshProUGUI InspectDefense;
    public TextMeshProUGUI InspectSpeed;
    
    [Header("References")]
    private GameManager gameManager;
    private ActionPanelManager actionPanelManager;
    private Selection selection;
    private TurnOrder turnOrder;
    
    // Inspect mode state
    private bool inspectMode = false;
    
    // Store the previously selected button index before entering inspect mode
    private int previousButtonSelectionIndex = 0;
    
    #region Public API
    
    /// <summary>
    /// Gets whether inspect mode is currently active
    /// </summary>
    public bool IsInspectMode()
    {
        return inspectMode;
    }
    
    #endregion
    
    #region Lifecycle Methods
    
    private void Start()
    {
        // Find references
        gameManager = FindFirstObjectByType<GameManager>();
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        selection = FindFirstObjectByType<Selection>();
        turnOrder = FindFirstObjectByType<TurnOrder>();
        
        // Initially hide the panel
        if (InspectPanel != null)
        {
            InspectPanel.SetActive(false);
        }
        
        // Subscribe to selection changes
        if (selection != null)
        {
            selection.OnSelectionChanged += OnSelectionChanged;
        }
    }
    
    private void Update()
    {
        // Only process inspect functionality if enabled
        if (!enableInspectFunctionality)
        {
            return;
        }
        
        // Handle inspect toggle input
        HandleInspectToggleInput();
        
        // Only handle navigation if in inspect mode
        if (inspectMode)
        {
            HandleInspectNavigation();
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from selection events
        if (selection != null)
        {
            selection.OnSelectionChanged -= OnSelectionChanged;
        }
    }
    
    #endregion
    
    #region Core Inspect Mode Methods
    
    /// <summary>
    /// Attempts to enter inspect mode (only allowed on player turn, not during animations)
    /// </summary>
    private void TryEnterInspectMode()
    {
        // Check if we can enter inspect mode
        if (!CanEnterInspectMode())
        {
            return;
        }
        
        EnterInspectMode();
    }
    
    /// <summary>
    /// Enters inspect mode
    /// </summary>
    private void EnterInspectMode()
    {
        if (inspectMode) return;
        
        // Store the current button selection before entering inspect mode
        // This allows us to restore it when exiting
        StoreCurrentButtonSelection();
        
        inspectMode = true;
        
        // Show inspect panel
        if (InspectPanel != null)
        {
            InspectPanel.SetActive(true);
        }
        
        // Setup selection for all alive units
        if (selection != null)
        {
            selection.SetupUnitSelection(UnitTargetType.AnyAlive);
        }
        
        // Hide user panel root which contains all UI elements
        if (gameManager != null)
        {
            gameManager.HideUserPanel();
        }
        
        // Update display with first selected unit
        UpdateInspectDisplay();
        
        Debug.Log("Entered Inspect Mode");
    }
    
    /// <summary>
    /// Exits inspect mode
    /// </summary>
    private void ExitInspectMode()
    {
        if (!inspectMode) return;
        
        inspectMode = false;
        
        // Hide inspect panel
        if (InspectPanel != null)
        {
            InspectPanel.SetActive(false);
        }
        
        // Clear selection (this will clear all markers)
        if (selection != null)
        {
            selection.ClearSelection();
        }
        
        // Restore action panels
        if (actionPanelManager != null)
        {
            actionPanelManager.ShowActionPanel();
            // ShowEndTurnButton will be handled by UpdatePanelVisibility
            actionPanelManager.UpdatePanelVisibility();
            
            // Force a selection update to ensure markers are properly restored
            // Use a coroutine to wait a frame, ensuring buttons are active in hierarchy before refreshing markers
            StartCoroutine(RefreshSelectionMarkersAfterExit());
        }
        
        Debug.Log("Exited Inspect Mode");
    }
    
    #endregion
    
    #region Input Handling
    
    /// <summary>
    /// Handles inspect toggle input (Q key)
    /// </summary>
    private void HandleInspectToggleInput()
    {
        // Toggle inspect mode with Q key
        if (Keyboard.current != null && Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            if (inspectMode)
            {
                ExitInspectMode();
            }
            else
            {
                TryEnterInspectMode();
            }
        }
    }
    
    /// <summary>
    /// Handles navigation in inspect mode (arrow keys/WASD, ESC/right-click/X to exit)
    /// </summary>
    private void HandleInspectNavigation()
    {
        // Exit with ESC, right-click, or X key
        if (ShouldExitInspectMode())
        {
            ExitInspectMode();
            return;
        }
        
        // Navigate with arrow keys or WASD
        NavigateInspectSelection();
    }
    
    /// <summary>
    /// Checks if inspect mode should be exited based on input
    /// </summary>
    private bool ShouldExitInspectMode()
    {
        return (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame) || 
               (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) || 
               (Keyboard.current != null && Keyboard.current[Key.X].wasPressedThisFrame);
    }
    
    /// <summary>
    /// Handles navigation input for inspect selection
    /// </summary>
    private void NavigateInspectSelection()
    {
        if (selection == null) return;
        
        if (Keyboard.current != null && (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || Keyboard.current[Key.A].wasPressedThisFrame))
        {
            selection.Previous();
        }
        else if (Keyboard.current != null && (Keyboard.current[Key.RightArrow].wasPressedThisFrame || Keyboard.current[Key.D].wasPressedThisFrame))
        {
            selection.Next();
        }
    }
    
    #endregion
    
    #region Display Management
    
    /// <summary>
    /// Updates the inspect display with the currently selected unit's information
    /// </summary>
    private void UpdateInspectDisplay()
    {
        if (selection == null) return;
        
        Unit selectedUnit = selection.GetSelectedUnit();
        
        if (selectedUnit == null)
        {
            ClearInspectDisplay();
            return;
        }
        
        PopulateInspectDisplay(selectedUnit);
    }
    
    /// <summary>
    /// Clears all inspect display fields
    /// </summary>
    private void ClearInspectDisplay()
    {
        if (InspectName != null) InspectName.text = "";
        if (InspectHP != null) InspectHP.text = "";
        if (InspectDefense != null) InspectDefense.text = "";
        if (InspectSpeed != null) InspectSpeed.text = "";
    }
    
    /// <summary>
    /// Populates inspect display with unit information
    /// </summary>
    private void PopulateInspectDisplay(Unit unit)
    {
        // Update name
        if (InspectName != null)
        {
            InspectName.text = unit.UnitName;
        }
        
        // Update HP (only show max health)
        if (InspectHP != null)
        {
            InspectHP.text = $"HP: {unit.MaxHP}";
        }
        
        // Update Defense
        if (InspectDefense != null)
        {
            InspectDefense.text = $"Defense: {unit.Defense}";
        }
        
        // Update Speed
        if (InspectSpeed != null)
        {
            InspectSpeed.text = $"Speed: {unit.Speed}";
        }
    }
    
    /// <summary>
    /// Called when selection changes
    /// </summary>
    private void OnSelectionChanged(object selectedItem)
    {
        if (inspectMode)
        {
            UpdateInspectDisplay();
        }
    }
    
    #endregion
    
    #region Reference Validation and Conditions
    
    /// <summary>
    /// Validates and ensures all required references are available
    /// </summary>
    private bool ValidateReferences()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return false;
        }
        
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
            if (actionPanelManager == null) return false;
        }
        
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
            if (selection == null) return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if inspect mode can be entered (player turn, not during animations, not inside SkillsPanel or ItemsPanel)
    /// </summary>
    private bool CanEnterInspectMode()
    {
        // Must have references
        if (!ValidateReferences())
        {
            return false;
        }
        
        // Check if it's a player's turn
        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null || !currentUnit.IsPlayerUnit)
        {
            return false;
        }
        
        // Check if animations are running (skill execution)
        if (actionPanelManager.IsSkillExecuting())
        {
            return false;
        }
        
        // Check if we're inside SkillsPanel or ItemsPanel - inspection mode should only be available from the outermost UI panel (ActionPanel)
        if (actionPanelManager.SkillsPanel != null && actionPanelManager.SkillsPanel.activeSelf)
        {
            return false;
        }
        
        if (actionPanelManager.ItemsPanel != null && actionPanelManager.ItemsPanel.activeSelf)
        {
            return false;
        }
        
        return true;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Stores the current button selection before entering inspect mode
    /// </summary>
    private void StoreCurrentButtonSelection()
    {
        if (selection == null || actionPanelManager == null)
        {
            return;
        }
        
        // Check if we're in button selection mode (ActionPanel is active)
        if (actionPanelManager.ActionPanel != null && actionPanelManager.ActionPanel.activeSelf)
        {
            // Check if the current selection is a button
            if (selection.IsValidSelection() && selection.CurrentSelection is Button currentButton)
            {
                // Find which button index it is
                Button[] buttons = new Button[] { actionPanelManager.SkillsButton, actionPanelManager.ItemsButton };
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == currentButton)
                    {
                        previousButtonSelectionIndex = i;
                        break;
                    }
                }
            }
            else if (selection.IsValidSelection())
            {
                // If there's a valid selection but it's not a button, store the index anyway
                previousButtonSelectionIndex = selection.CurrentIndex;
            }
        }
    }
    
    /// <summary>
    /// Coroutine to refresh selection markers after exiting inspect mode
    /// Waits a frame to ensure buttons are active in hierarchy before refreshing
    /// </summary>
    private IEnumerator RefreshSelectionMarkersAfterExit()
    {
        // Wait one frame to ensure ActionPanel and buttons are fully active
        yield return null;
        
        // Selection markers will be updated automatically by ActionPanelManager
    }
    
    #endregion
}
