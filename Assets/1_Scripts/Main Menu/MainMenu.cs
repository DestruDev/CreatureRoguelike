using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
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
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Button Container")]
    [SerializeField] private GameObject buttonsContainer;

    void Start()
    {
        // Initialize panels - hide all panels, show buttons
        ShowMainButtons();
        HideAllPanels();
        
        // Set up button listeners
        SetupButtonListeners();
    }

    void Update()
    {
        // Check for ESC key to go back from panels
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (IsAnyPanelActive())
            {
                OnBackButtonClicked();
            }
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

    public void OnPlayButtonClicked()
    {
        HideMainButtons();
        ShowPanel(characterSelectPanel);
    }

    public void OnSettingsButtonClicked()
    {
        HideMainButtons();
        ShowPanel(settingsPanel);
    }

    public void OnAchievementsButtonClicked()
    {
        HideMainButtons();
        ShowPanel(achievementsPanel);
    }

    public void OnExitButtonClicked()
    {
        ExitGame();
    }

    public void OnBackButtonClicked()
    {
        HideAllPanels();
        ShowMainButtons();
    }

    public void OnStartGameButtonClicked()
    {
        LoadGameScene();
    }

    private void ShowMainButtons()
    {
        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(true);
        }
    }

    private void HideMainButtons()
    {
        if (buttonsContainer != null)
        {
            buttonsContainer.SetActive(false);
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void HideAllPanels()
    {
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (achievementsPanel != null)
            achievementsPanel.SetActive(false);
    }

    private bool IsAnyPanelActive()
    {
        return (characterSelectPanel != null && characterSelectPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf) ||
               (achievementsPanel != null && achievementsPanel.activeSelf);
    }

    private void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
