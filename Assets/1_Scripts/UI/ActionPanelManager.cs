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
    
    // Track the last unit to detect when a new turn starts
    private Unit lastUnit = null;
    
    // Track if we're in skill execution mode (to block UI from showing)
    private bool isSkillExecuting = false;
    
    // Track if panels have been hidden for game over
    private bool panelsHiddenForGameOver = false;

    private void Start()
    {
        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();

        // Find TurnOrder
        turnOrder = FindFirstObjectByType<TurnOrder>();
        
        // Find InspectPanelManager
        inspectPanelManager = FindFirstObjectByType<InspectPanelManager>();

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
    }

    private void Update()
    {
        // Don't process if in inspect mode
        if (IsInInspectMode())
        {
            return;
        }
        
        // Check for ESC key press or right-click to return to ActionPanel
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            GoBackToActionPanel();
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
        
        // Hide all panels first
        HideAllPanels();

        // Show SkillsPanel
        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(true);
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
        
        // Hide all panels first
        HideAllPanels();

        // Show ItemsPanel
        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(true);
        }
        
        // Update item panel display
        ItemPanelManager itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        if (itemPanelManager != null)
        {
            itemPanelManager.UpdateItems();
        }
        
        // Update back button visibility
        UpdateBackButtonVisibility();
    }

    public void HideAllPanels()
    {
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
}
