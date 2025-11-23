using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InGameMenu : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button settingsBackButton;

    [Header("Main Menu")]
    [SerializeField] private Button backToMainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private ActionPanelManager actionPanelManager;
    private Selection selection;
    private bool panelsWereActiveThisFrame = false; // Track if panels were active at start of frame

    void Start()
    {
        // Find ActionPanelManager
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        
        // Find Selection
        selection = FindFirstObjectByType<Selection>();
        
        SetupButtonListeners();
        HideSettingsPanel();
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (IsSettingsPanelActive())
            {
                HideSettingsPanel();
            }
        }
    }

    private void HandleEscapeKey()
    {
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

    private void LoadMainMenuScene()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
