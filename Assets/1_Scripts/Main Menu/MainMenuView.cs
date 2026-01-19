using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button exitButton;

    [Header("Panels")]
    [SerializeField] private GameObject characterSelectPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject achievementsPanel;

    [Header("Back Buttons")]
    [SerializeField] private Button characterSelectBackButton;
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button achievementsBackButton;

    [Header("Start Game")]
    [SerializeField] private Button startGameButton;

    [Header("Button Container")]
    [SerializeField] private GameObject buttonsContainer;

    [Header("Version Text")]
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private string versionString = "Version 1.0";
    
    [Header("Save Status")]
    [SerializeField] private TextMeshProUGUI saveStatusText;

    private MainMenuController controller;

    private void Awake()
    {
        controller = GetComponent<MainMenuController>();
    }

    private void OnEnable()
    {
        SetupButtonListeners();
        UpdateSaveStatus();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    void Start()
    {
        // Initialize panels - hide all panels, show buttons
        ShowMainButtons();
        HideAllPanels();
        
        // Set version text
        if (versionText != null)
        {
            versionText.text = versionString;
        }
        
        // Update save status
        UpdateSaveStatus();
    }
    
    /// <summary>
    /// Updates the save status text to show YES if save exists, NO if it doesn't
    /// Green for YES, Red for NO
    /// </summary>
    public void UpdateSaveStatus()
    {
        if (saveStatusText != null)
        {
            // Check if map save data exists
            bool hasSave = PlayerPrefs.HasKey("Map");
            saveStatusText.text = hasSave ? "YES" : "NO";
            
            // Set color: Green for YES, Red for NO
            saveStatusText.color = hasSave ? Color.green : Color.red;
        }
    }

    private void SetupButtonListeners()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        if (achievementsButton != null)
            achievementsButton.onClick.AddListener(OnAchievementsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);

        // Set up back button listeners
        if (characterSelectBackButton != null)
            characterSelectBackButton.onClick.AddListener(OnBackButtonClicked);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnBackButtonClicked);

        if (achievementsBackButton != null)
            achievementsBackButton.onClick.AddListener(OnBackButtonClicked);

        // Set up start game button
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
    }

    private void RemoveButtonListeners()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButtonClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

        if (achievementsButton != null)
            achievementsButton.onClick.RemoveListener(OnAchievementsButtonClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitButtonClicked);

        if (characterSelectBackButton != null)
            characterSelectBackButton.onClick.RemoveListener(OnBackButtonClicked);

        if (settingsBackButton != null)
            settingsBackButton.onClick.RemoveListener(OnBackButtonClicked);

        if (achievementsBackButton != null)
            achievementsBackButton.onClick.RemoveListener(OnBackButtonClicked);

        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartGameButtonClicked);
    }

    // Button click handlers - delegate to controller
    private void OnPlayButtonClicked()
    {
        if (controller != null)
        {
            controller.OnPlayButtonClicked();
        }
    }

    private void OnSettingsButtonClicked()
    {
        if (controller != null)
        {
            controller.OnSettingsButtonClicked();
        }
    }

    private void OnAchievementsButtonClicked()
    {
        if (controller != null)
        {
            controller.OnAchievementsButtonClicked();
        }
    }

    private void OnExitButtonClicked()
    {
        if (controller != null)
        {
            controller.OnExitButtonClicked();
        }
    }

    private void OnBackButtonClicked()
    {
        if (controller != null)
        {
            controller.OnBackButtonClicked();
        }
    }

    private void OnStartGameButtonClicked()
    {
        if (controller != null)
        {
            controller.OnStartGameButtonClicked();
        }
    }

    // UI show/hide methods
    public void ShowMainButtons()
    {
        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(true);
        }
    }

    public void HideMainButtons()
    {
        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(false);
        }
    }

    public void ShowPanel(GameObject panel)
    {
        // Always hide all panels first to ensure only ONE panel is active at a time
        HideAllPanels();
        
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void HideAllPanels()
    {
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    public bool IsAnyPanelActive()
    {
        return (characterSelectPanel != null && characterSelectPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf) ||
               (achievementsPanel != null && achievementsPanel.activeSelf);
    }

    // Expose panel references for controller
    public GameObject CharacterSelectPanel => characterSelectPanel;
    public GameObject SettingsPanel => settingsPanel;
    public GameObject AchievementsPanel => achievementsPanel;
}
