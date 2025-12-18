using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Start Game")]
    [SerializeField] private string gameSceneName = "GameScene";

    private MainMenuView view;

    void Start()
    {
        view = GetComponent<MainMenuView>();
    }

    void Update()
    {
        // Check for ESC key to go back from panels
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (view != null && view.IsAnyPanelActive())
            {
                OnBackButtonClicked();
            }
        }
    }

    // Button click handlers
    public void OnPlayButtonClicked()
    {
        if (view != null)
        {
            view.HideMainButtons();
            view.ShowPanel(view.CharacterSelectPanel);
        }
    }

    public void OnSettingsButtonClicked()
    {
        if (view != null)
        {
            view.HideMainButtons();
            view.ShowPanel(view.SettingsPanel);
        }
    }

    public void OnAchievementsButtonClicked()
    {
        if (view != null)
        {
            view.HideMainButtons();
            view.ShowPanel(view.AchievementsPanel);
        }
    }

    public void OnExitButtonClicked()
    {
        ExitGame();
    }

    public void OnBackButtonClicked()
    {
        if (view != null)
        {
            view.HideAllPanels();
            view.ShowMainButtons();
        }
    }

    public void OnStartGameButtonClicked()
    {
        LoadGameScene();
    }

    // Scene loading and quit logic
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
