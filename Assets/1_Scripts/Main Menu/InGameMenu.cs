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

    void Start()
    {
        // Find ActionPanelManager
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        
        // Find Selection
        selection = FindFirstObjectByType<Selection>();
        
        SetupButtonListeners();
        HideSettingsPanel();
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
        // Check for ESC key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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
