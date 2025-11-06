using UnityEngine;
using TMPro;

public class InspectPanelManager : MonoBehaviour
{
    [Header("Panel")]
    public GameObject InspectPanel;
    
    [Header("Settings")]
    public bool enableInspectFunctionality = true;
    
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
        
        // Toggle inspect mode with Q key
        if (Input.GetKeyDown(KeyCode.Q))
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
        
        // Only handle navigation if in inspect mode
        if (inspectMode)
        {
            HandleInspectNavigation();
        }
    }
    
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
    /// Checks if inspect mode can be entered (player turn, not during animations)
    /// </summary>
    private bool CanEnterInspectMode()
    {
        // Must have references
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
        
        return true;
    }
    
    /// <summary>
    /// Enters inspect mode
    /// </summary>
    private void EnterInspectMode()
    {
        if (inspectMode) return;
        
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
        
        // Clear selection
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
        }
        
        Debug.Log("Exited Inspect Mode");
    }
    
    /// <summary>
    /// Handles navigation in inspect mode (arrow keys/WASD, ESC/right-click to exit)
    /// </summary>
    private void HandleInspectNavigation()
    {
        // Exit with ESC or right-click
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitInspectMode();
            return;
        }
        
        // Navigate with arrow keys or WASD
        if (selection != null)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                selection.Previous();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                selection.Next();
            }
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
    
    /// <summary>
    /// Updates the inspect display with the currently selected unit's information
    /// </summary>
    private void UpdateInspectDisplay()
    {
        if (selection == null) return;
        
        Unit selectedUnit = selection.GetSelectedUnit();
        
        if (selectedUnit == null)
        {
            // Clear display if no unit selected
            if (InspectName != null) InspectName.text = "";
            if (InspectHP != null) InspectHP.text = "";
            if (InspectDefense != null) InspectDefense.text = "";
            if (InspectSpeed != null) InspectSpeed.text = "";
            return;
        }
        
        // Update name
        if (InspectName != null)
        {
            InspectName.text = selectedUnit.UnitName;
        }
        
        // Update HP
        if (InspectHP != null)
        {
            InspectHP.text = $"HP: {selectedUnit.CurrentHP} / {selectedUnit.MaxHP}";
        }
        
        // Update Defense
        if (InspectDefense != null)
        {
            InspectDefense.text = $"Defense: {selectedUnit.Defense}";
        }
        
        // Update Speed
        if (InspectSpeed != null)
        {
            InspectSpeed.text = $"Speed: {selectedUnit.Speed}";
        }
    }
    
    /// <summary>
    /// Gets whether inspect mode is currently active
    /// </summary>
    public bool IsInspectMode()
    {
        return inspectMode;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from selection events
        if (selection != null)
        {
            selection.OnSelectionChanged -= OnSelectionChanged;
        }
    }
}
