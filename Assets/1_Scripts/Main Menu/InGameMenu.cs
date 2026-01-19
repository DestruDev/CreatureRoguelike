using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class InGameMenu : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Main Menu")]
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private Button abandonRunButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private ActionPanelManager actionPanelManager;
    private Selection selection;
    private bool panelsWereActiveThisFrame = false; // Track if panels were active at start of frame
    
    // Static reference to check if settings panel is active from other scripts
    private static InGameMenu instance;

    void Start()
    {
        // Set static instance
        instance = this;
        
        // Find ActionPanelManager
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        
        // Find Selection
        selection = FindFirstObjectByType<Selection>();
        
        SetupButtonListeners();
        HideSettingsPanel();
    }
    
    void OnDestroy()
    {
        // Clear static instance when destroyed
        if (instance == this)
        {
            instance = null;
        }
    }
    
    /// <summary>
    /// Static method to check if settings panel is active (called from ActionPanelManager)
    /// </summary>
    public static bool IsSettingsPanelActiveStatic()
    {
        return instance != null && instance.IsSettingsPanelActive();
    }
    
    void LateUpdate()
    {
        // Reset the flag at the end of the frame
        panelsWereActiveThisFrame = false;
    }

    private void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        
        if (selection != null)
        {
            selection.SetMarkersRenderInFront(true);
            // Reset marker parent to default (null = use canvas)
            selection.SetMarkerParent(null);
            // Re-enable navigation when settings panel is closed
            selection.SetNavigationEnabled(true);
        }
    }

    private void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        
        if (selection != null)
        {
            selection.SetMarkersRenderInFront(false);
            // Set marker parent - uses markerParent from Selection.cs if set, otherwise uses settings panel
            if (settingsPanel != null)
            {
                selection.SetMarkerParent(settingsPanel.transform);
            }
            // Disable navigation when settings panel is open (marker stays visible but won't move)
            selection.SetNavigationEnabled(false);
        }
    }

    public void OnSettingsButtonClicked()
    {
        // Don't open settings if round end panel is active
        if (IsRoundEndPanelActive())
        {
            return;
        }
        
        // Toggle settings panel - if open, close it; if closed, open it
        if (IsSettingsPanelActive())
        {
            HideSettingsPanel();
        }
        else
        {
            ShowSettingsPanel();
        }
    }

    void Update()
    {
        // Ensure we have a reference to ActionPanelManager
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        }
        
        // Check panel state at the START of the frame (before ActionPanelManager might close them)
        bool skillsPanelActive = actionPanelManager != null && 
                                actionPanelManager.SkillsPanel != null && 
                                actionPanelManager.SkillsPanel.activeSelf;
        bool itemsPanelActive = actionPanelManager != null && 
                               actionPanelManager.ItemsPanel != null && 
                               actionPanelManager.ItemsPanel.activeSelf;
        
        // Track if panels were active this frame
        panelsWereActiveThisFrame = skillsPanelActive || itemsPanelActive;
        
        // Check for ESC key
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            // Priority 1: If settings panel is open, close it (highest priority)
            if (IsSettingsPanelActive())
            {
                HideSettingsPanel();
                return; // Settings panel closed, don't process further
            }
            
            // Don't handle ESC if ActionPanelManager already handled it (e.g., when panels are active)
            if (ActionPanelManager.EscHandledThisFrame)
            {
                return; // ActionPanelManager already handled ESC
            }
            
            // Re-check panel state when ESC is pressed (in case it changed)
            // Use the variables already declared at the start of Update()
            bool panelsActive = (actionPanelManager != null && 
                                actionPanelManager.SkillsPanel != null && 
                                actionPanelManager.SkillsPanel.activeSelf) ||
                               (actionPanelManager != null && 
                                actionPanelManager.ItemsPanel != null && 
                                actionPanelManager.ItemsPanel.activeSelf);
            
            // Don't handle ESC if SkillsPanel or ItemsPanel are active - let ActionPanelManager handle it
            if (panelsActive)
            {
                return; // Let ActionPanelManager handle ESC to go back to ActionPanel
            }
            
            HandleEscapeKey();
        }
        
        // Check for X key to close settings panel
        if (Keyboard.current != null && Keyboard.current[Key.X].wasPressedThisFrame)
        {
            if (IsSettingsPanelActive())
            {
                HideSettingsPanel();
            }
        }
    }

    private void HandleEscapeKey()
    {
        // Don't open settings if round end panel is active
        if (IsRoundEndPanelActive())
        {
            return;
        }
        
        // If settings panel is open, close it
        if (IsSettingsPanelActive())
        {
            HideSettingsPanel();
        }
        // If not in any other panels (SkillsPanel/ItemsPanel), open settings panel
        else if (!IsAnyOtherPanelActive())
        {
            ShowSettingsPanel();
        }
    }
    
    /// <summary>
    /// Checks if the round end panel is currently active
    /// </summary>
    private bool IsRoundEndPanelActive()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        return gameManager != null && gameManager.roundEndPanel != null && gameManager.roundEndPanel.activeSelf;
    }

    private bool IsSettingsPanelActive()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }

    private bool IsAnyOtherPanelActive()
    {
        if (actionPanelManager == null)
            return false;
            
        return (actionPanelManager.SkillsPanel != null && actionPanelManager.SkillsPanel.activeSelf) ||
               (actionPanelManager.ItemsPanel != null && actionPanelManager.ItemsPanel.activeSelf);
    }

    private void SetupButtonListeners()
    {
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnSettingsBackButtonClicked);

        if (backToMainMenuButton != null)
            backToMainMenuButton.onClick.AddListener(OnBackToMainMenuButtonClicked);
            
        if (abandonRunButton != null)
            abandonRunButton.onClick.AddListener(OnAbandonRunButtonClicked);
    }

    public void OnSettingsBackButtonClicked()
    {
        // You can call functions from MainMenu.cs here if needed
        HideSettingsPanel();
    }

    public void OnBackToMainMenuButtonClicked()
    {
        LoadMainMenuScene();
    }
    
    /// <summary>
    /// Called when the abandon run button is clicked - deletes map save data and returns to main menu
    /// </summary>
    public void OnAbandonRunButtonClicked()
    {
        AbandonRun();
    }
    
    /// <summary>
    /// Deletes the map save data, resets gold to starting value, and loads the main menu scene
    /// </summary>
    private void AbandonRun()
    {
        // Delete map save data
        if (PlayerPrefs.HasKey("Map"))
        {
            PlayerPrefs.DeleteKey("Map");
            Debug.Log("Abandon Run: Map save data deleted.");
        }
        
        // Reset gold to starting value
        ResetGoldToStartingValue();
        
        // Save all changes
        PlayerPrefs.Save();
        
        // Load main menu
        LoadMainMenuScene();
    }
    
    /// <summary>
    /// Resets the player's gold to the starting gold value from GameManager
    /// </summary>
    private void ResetGoldToStartingValue()
    {
        // Find Inventory component
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("InGameMenu: Inventory component not found! Cannot reset gold.");
            return;
        }
        
        // Get starting gold from GameManager
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        int startingGold = 0;
        if (gameManager != null)
        {
            startingGold = gameManager.StartingGold;
        }
        else
        {
            Debug.LogWarning("InGameMenu: GameManager not found! Using default starting gold of 0.");
        }
        
        // Reset gold to starting value
        inventory.SetCurrency(startingGold);
        
        // Delete saved currency from PlayerPrefs so it doesn't reload the old value
        if (PlayerPrefs.HasKey("PlayerCurrency"))
        {
            PlayerPrefs.DeleteKey("PlayerCurrency");
            Debug.Log($"Abandon Run: Currency save data deleted. Gold reset to starting value: {startingGold}");
        }
        else
        {
            Debug.Log($"Abandon Run: Gold reset to starting value: {startingGold}");
        }
    }

    private void LoadMainMenuScene()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
