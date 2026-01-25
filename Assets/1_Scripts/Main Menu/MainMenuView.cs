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
    
    [Header("Profile Name Display")]
    [SerializeField] private TextMeshProUGUI nameDisplayText;
    
    [Header("Name Input Panel")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button nameSubmitButton;
    
    [Header("Profile Edit Panel")]
    [SerializeField] private GameObject profileEditPanel;
    [SerializeField] private Button openProfileEditPanelButton;
    
    [Header("Profile List Panel")]
    [SerializeField] private GameObject profileListPanel;
    [SerializeField] private Button openProfileListPanelButton;
    [SerializeField] private Button profileListBackButton;
    
    // Profile slot UI elements (5 slots)
    [Header("Profile Slots")]
    [SerializeField] private TextMeshProUGUI[] profileSlotNames = new TextMeshProUGUI[5];
    [SerializeField] private Button[] profileSlotSelectButtons = new Button[5];
    [SerializeField] private Button[] profileSlotDeleteButtons = new Button[5];
    [SerializeField] private GameObject[] profileSlotEmptyIndicators = new GameObject[5];

    private MainMenuController controller;

    private void Awake()
    {
        controller = GetComponent<MainMenuController>();
    }

    private void OnEnable()
    {
        SetupButtonListeners();
        UpdateSaveStatus();
        UpdateNameDisplay();
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
        
        // Hide name input panel initially
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false);
        }
        
        // Hide profile edit panel initially
        if (profileEditPanel != null)
        {
            profileEditPanel.SetActive(false);
        }
        
        // Hide profile list panel initially
        if (profileListPanel != null)
        {
            profileListPanel.SetActive(false);
        }
        
        // Set up name input submit button
        if (nameSubmitButton != null)
        {
            nameSubmitButton.onClick.AddListener(OnNameSubmitClicked);
        }
        
        // Set up Enter key submission for input field
        if (nameInputField != null)
        {
            nameInputField.onSubmit.AddListener((string value) => OnNameSubmitClicked());
        }
        
        // Set version text
        if (versionText != null)
        {
            versionText.text = versionString;
        }
        
        // Update save status
        UpdateSaveStatus();
        
        // Update name display
        UpdateNameDisplay();
        
        // Update profile list display
        UpdateProfileList();
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
            saveStatusText.text = hasSave ? "Existing Save : YES" : "Existing Save : NO";
            
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
        
        // Set up profile edit panel button
        if (openProfileEditPanelButton != null)
            openProfileEditPanelButton.onClick.AddListener(OnOpenProfileEditPanelClicked);
        
        // Set up profile list panel button
        if (openProfileListPanelButton != null)
            openProfileListPanelButton.onClick.AddListener(OnOpenProfileListPanelClicked);
        
        // Set up profile list back button
        if (profileListBackButton != null)
            profileListBackButton.onClick.AddListener(OnProfileListBackClicked);
        
        // Set up profile slot buttons
        for (int i = 0; i < profileSlotSelectButtons.Length && i < 5; i++)
        {
            int index = i; // Capture for closure
            if (profileSlotSelectButtons[i] != null)
            {
                profileSlotSelectButtons[i].onClick.AddListener(() => OnProfileSlotSelected(index));
            }
            if (profileSlotDeleteButtons[i] != null)
            {
                profileSlotDeleteButtons[i].onClick.AddListener(() => OnProfileSlotDeleted(index));
            }
        }
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
        
        if (openProfileEditPanelButton != null)
            openProfileEditPanelButton.onClick.RemoveListener(OnOpenProfileEditPanelClicked);
        
        if (openProfileListPanelButton != null)
            openProfileListPanelButton.onClick.RemoveListener(OnOpenProfileListPanelClicked);
        
        if (profileListBackButton != null)
            profileListBackButton.onClick.RemoveListener(OnProfileListBackClicked);
        
        // Remove profile slot button listeners (clear all since we can't easily remove lambda listeners)
        for (int i = 0; i < profileSlotSelectButtons.Length && i < 5; i++)
        {
            if (profileSlotSelectButtons[i] != null)
            {
                profileSlotSelectButtons[i].onClick.RemoveAllListeners();
            }
            if (profileSlotDeleteButtons[i] != null)
            {
                profileSlotDeleteButtons[i].onClick.RemoveAllListeners();
            }
        }
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
    
    private void OnOpenProfileEditPanelClicked()
    {
        if (controller != null)
        {
            controller.OnOpenProfileEditPanelClicked();
        }
    }
    
    private void OnOpenProfileListPanelClicked()
    {
        if (controller != null)
        {
            controller.OnOpenProfileListPanelClicked();
        }
    }
    
    private void OnProfileListBackClicked()
    {
        if (profileListPanel != null)
        {
            HideProfileListPanel();
        }
    }
    
    private void OnProfileSlotSelected(int index)
    {
        if (controller != null)
        {
            controller.OnProfileSelected(index);
        }
    }
    
    private void OnProfileSlotDeleted(int index)
    {
        if (controller != null)
        {
            controller.OnProfileDeleted(index);
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
    
    // Name input panel methods
    public bool HasNameInputPanel()
    {
        return nameInputPanel != null;
    }
    
    public void ShowNameInputPanel(bool clearInput = false)
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            // Hide main buttons while name input is shown
            HideMainButtons();
            // Clear input field if requested (for creating new profile)
            if (clearInput && nameInputField != null)
            {
                nameInputField.text = "";
            }
            // Focus on input field
            if (nameInputField != null)
            {
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }
        }
    }
    
    public void HideNameInputPanel()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(false);
            // Show main buttons again
            ShowMainButtons();
        }
    }
    
    public bool IsNameInputPanelActive()
    {
        return nameInputPanel != null && nameInputPanel.activeSelf;
    }
    
    private void OnNameSubmitClicked()
    {
        string enteredName = "";
        if (nameInputField != null)
        {
            enteredName = nameInputField.text;
        }
        
        // Validate and submit name
        if (string.IsNullOrWhiteSpace(enteredName))
        {
            enteredName = "Player"; // Default name if empty
        }
        
        if (controller != null)
        {
            controller.OnNameSubmitted(enteredName);
        }
        
        // Update name display after submission
        UpdateNameDisplay();
    }
    
    /// <summary>
    /// Updates the name display text to show the tmp profile name, or "guest" if no profile exists
    /// </summary>
    public void UpdateNameDisplay()
    {
        if (nameDisplayText != null)
        {
            string profileName = SaveProfiles.GetTmpProfileName();
            if (string.IsNullOrEmpty(profileName))
            {
                profileName = SaveProfiles.ProfileName; // Fallback to PlayerPrefs if tmp is empty
            }
            
            // If still no profile name, display "guest"
            if (string.IsNullOrEmpty(profileName) || !SaveProfiles.HasProfile())
            {
                profileName = "guest";
            }
            
            nameDisplayText.text = profileName;
        }
    }

    // Profile Edit Panel methods
    public void ShowProfileEditPanel()
    {
        if (profileEditPanel != null)
        {
            profileEditPanel.SetActive(true);
        }
    }
    
    public void HideProfileEditPanel()
    {
        if (profileEditPanel != null)
        {
            profileEditPanel.SetActive(false);
        }
    }
    
    public GameObject GetProfileEditPanel()
    {
        return profileEditPanel;
    }
    
    public bool IsProfileEditPanelActive()
    {
        return profileEditPanel != null && profileEditPanel.activeSelf;
    }
    
    // Profile List Panel methods
    public void ShowProfileListPanel()
    {
        if (profileListPanel != null)
        {
            profileListPanel.SetActive(true);
            HideMainButtons();
            UpdateProfileList(); // Refresh the list when showing
        }
    }
    
    public void HideProfileListPanel()
    {
        if (profileListPanel != null)
        {
            profileListPanel.SetActive(false);
            ShowMainButtons();
        }
    }
    
    public bool IsProfileListPanelActive()
    {
        return profileListPanel != null && profileListPanel.activeSelf;
    }
    
    /// <summary>
    /// Updates the profile list UI to show all 5 slots with their current state
    /// </summary>
    public void UpdateProfileList()
    {
        int activeIndex = ProfileListManager.ActiveProfileIndex;
        
        for (int i = 0; i < 5; i++)
        {
            bool hasProfile = ProfileListManager.HasProfileAt(i);
            string profileName = ProfileListManager.GetProfileName(i);
            bool isActive = (i == activeIndex);
            
            // Update profile name text
            if (profileSlotNames != null && i < profileSlotNames.Length && profileSlotNames[i] != null)
            {
                if (hasProfile)
                {
                    profileSlotNames[i].text = profileName;
                    profileSlotNames[i].color = isActive ? Color.green : Color.white;
                }
                else
                {
                    profileSlotNames[i].text = "Empty Slot";
                    profileSlotNames[i].color = Color.gray;
                }
            }
            
            // Show/hide empty indicator
            if (profileSlotEmptyIndicators != null && i < profileSlotEmptyIndicators.Length && profileSlotEmptyIndicators[i] != null)
            {
                profileSlotEmptyIndicators[i].SetActive(!hasProfile);
            }
            
            // Enable select button for both existing profiles and empty slots (empty slots trigger creation)
            if (profileSlotSelectButtons != null && i < profileSlotSelectButtons.Length && profileSlotSelectButtons[i] != null)
            {
                profileSlotSelectButtons[i].interactable = true;
            }
            
            // Enable/disable delete button (only if profile exists)
            if (profileSlotDeleteButtons != null && i < profileSlotDeleteButtons.Length && profileSlotDeleteButtons[i] != null)
            {
                profileSlotDeleteButtons[i].interactable = hasProfile;
            }
        }
    }

    // Expose panel references for controller
    public GameObject CharacterSelectPanel => characterSelectPanel;
    public GameObject SettingsPanel => settingsPanel;
    public GameObject AchievementsPanel => achievementsPanel;
}
