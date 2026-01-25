using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Start Game")]
    [SerializeField] private string gameSceneName = "GameScene";

    private MainMenuView view;
    private bool hasCheckedFirstTime = false;
    private int pendingProfileIndex = -1; // Track which slot index is pending creation

    void Start()
    {
        view = GetComponent<MainMenuView>();
        
        // Check if this is the first time entering main menu
        CheckFirstTimeEntry();
    }
    
    private void CheckFirstTimeEntry()
    {
        if (!hasCheckedFirstTime && SaveProfiles.IsFirstTime())
        {
            hasCheckedFirstTime = true;
            // Show name input panel if available, otherwise use default
            if (view != null && view.HasNameInputPanel())
            {
                view.ShowNameInputPanel();
            }
            else
            {
                // Fallback: use default name if no UI panel is set up
                SaveProfiles.SetProfileName("Player");
                if (view != null)
                {
                    view.UpdateNameDisplay(); // Update the name display
                }
            }
        }
    }
    
    // Called from UI when name is submitted
    public void OnNameSubmitted(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            // If we have a pending profile index, create the profile at that specific index
            if (pendingProfileIndex >= 0)
            {
                if (ProfileListManager.AddProfileAt(pendingProfileIndex, name))
                {
                    if (view != null)
                    {
                        view.HideNameInputPanel();
                        view.UpdateNameDisplay();
                        view.UpdateProfileList();
                        view.UpdateSaveStatus(); // Update save status for the new profile
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to create profile at index {pendingProfileIndex}");
                }
                pendingProfileIndex = -1; // Clear pending index
            }
            else
            {
                // Legacy behavior: check if we're at max capacity (only if this is a new profile creation)
                if (ProfileListManager.IsFull() && ProfileListManager.ActiveProfileIndex < 0)
                {
                    Debug.LogWarning("Cannot create profile - all 5 slots are full");
                    // TODO: Show error message to user if you have a UI for that
                    return;
                }
                
                SaveProfiles.SetProfileName(name);
                if (view != null)
                {
                    view.HideNameInputPanel();
                    view.UpdateNameDisplay(); // Update the name display after setting profile name
                    view.UpdateProfileList(); // Update profile list if panel is visible
                    view.UpdateSaveStatus(); // Update save status for the new/selected profile
                }
            }
        }
    }

    void Update()
    {
        // Check for ESC key to go back from panels
        if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (view != null)
            {
                // First check if name input panel is open
                if (view.IsNameInputPanelActive())
                {
                    view.HideNameInputPanel();
                    pendingProfileIndex = -1; // Clear pending index if user cancels
                }
                // Then check if profile list panel is open
                else if (view.IsProfileListPanelActive())
                {
                    view.HideProfileListPanel();
                }
                // Then check if profile edit panel is open
                else if (view.IsProfileEditPanelActive())
                {
                    view.HideProfileEditPanel();
                }
                // Then check other panels
                else if (view.IsAnyPanelActive())
                {
                    OnBackButtonClicked();
                }
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
    
    public void OnOpenProfileEditPanelClicked()
    {
        if (view != null)
        {
            view.ShowProfileEditPanel();
        }
    }
    
    /// <summary>
    /// Called when user selects a profile from the list
    /// </summary>
    public void OnProfileSelected(int profileIndex)
    {
        if (ProfileListManager.HasProfileAt(profileIndex))
        {
            // Profile exists, select it
            SaveProfiles.SelectProfile(profileIndex);
            if (view != null)
            {
                view.UpdateNameDisplay();
                view.UpdateProfileList();
                view.UpdateSaveStatus(); // Update save status to reflect the new profile's save state
            }
        }
        else
        {
            // Empty slot clicked - show name input to create a profile at this index
            pendingProfileIndex = profileIndex;
            if (view != null)
            {
                view.ShowNameInputPanel(clearInput: true);
            }
        }
    }
    
    /// <summary>
    /// Called when user deletes a profile at a specific index
    /// </summary>
    public void OnProfileDeleted(int profileIndex)
    {
        if (ProfileListManager.HasProfileAt(profileIndex))
        {
            SaveProfiles.DeleteProfile(profileIndex);
            if (view != null)
            {
                view.UpdateNameDisplay();
                view.UpdateProfileList();
                view.UpdateSaveStatus(); // Update save status after profile deletion
            }
        }
    }
    
    /// <summary>
    /// Called when user wants to open the profile list panel
    /// </summary>
    public void OnOpenProfileListPanelClicked()
    {
        if (view != null)
        {
            view.ShowProfileListPanel();
        }
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
