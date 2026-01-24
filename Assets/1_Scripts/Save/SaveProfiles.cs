using UnityEngine;

/// <summary>
/// Manages the currently active profile and profile-scoped save data.
/// Works in conjunction with ProfileListManager to handle multiple profiles.
/// </summary>
public class SaveProfiles : MonoBehaviour
{
    private const string FIRST_TIME_KEY = "FirstTimeMainMenu";
    
    // Temporary profile name variable (for display purposes)
    private static string tmpProfileName = "";
    
    /// <summary>
    /// Gets the name of the currently active profile from ProfileListManager
    /// </summary>
    public static string ProfileName
    {
        get
        {
            string activeName = ProfileListManager.ActiveProfileName;
            if (!string.IsNullOrEmpty(activeName))
            {
                return activeName;
            }
            // Fallback for legacy single-profile support
            return tmpProfileName;
        }
    }
    
    /// <summary>
    /// Gets the active profile index, or -1 if none selected
    /// </summary>
    public static int ActiveProfileIndex => ProfileListManager.ActiveProfileIndex;
    
    /// <summary>
    /// Check if this is the first time entering main menu
    /// </summary>
    public static bool IsFirstTime()
    {
        return !PlayerPrefs.HasKey(FIRST_TIME_KEY);
    }
    
    /// <summary>
    /// Sets the profile name by adding it to the profile list (if it doesn't exist) or selecting an existing one.
    /// If no profile is selected, creates a new profile.
    /// </summary>
    public static void SetProfileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        
        tmpProfileName = name;
        
        // Check if profile already exists
        int existingIndex = -1;
        for (int i = 0; i < ProfileListManager.MaxProfiles; i++)
        {
            if (ProfileListManager.GetProfileName(i) == name)
            {
                existingIndex = i;
                break;
            }
        }
        
        if (existingIndex >= 0)
        {
            // Profile exists, select it
            ProfileListManager.SelectProfile(existingIndex);
        }
        else
        {
            // Profile doesn't exist, add it
            int newIndex = ProfileListManager.AddProfile(name);
            if (newIndex >= 0)
            {
                ProfileListManager.SelectProfile(newIndex);
            }
        }
        
        PlayerPrefs.SetInt(FIRST_TIME_KEY, 1); // Mark that we've been here
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Gets the temporary profile name (for display purposes)
    /// </summary>
    public static string GetTmpProfileName()
    {
        string activeName = ProfileListManager.ActiveProfileName;
        if (!string.IsNullOrEmpty(activeName))
        {
            return activeName;
        }
        return tmpProfileName;
    }
    
    /// <summary>
    /// Check if a profile exists (either active profile or legacy single profile)
    /// </summary>
    public static bool HasProfile()
    {
        return ProfileListManager.ActiveProfileIndex >= 0 || !string.IsNullOrEmpty(tmpProfileName);
    }
    
    /// <summary>
    /// Deletes the currently active profile
    /// </summary>
    public static void DeleteProfile()
    {
        int activeIndex = ProfileListManager.ActiveProfileIndex;
        if (activeIndex >= 0)
        {
            ProfileListManager.DeleteProfile(activeIndex);
        }
        
        tmpProfileName = "";
        PlayerPrefs.DeleteKey(FIRST_TIME_KEY);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Deletes a profile at a specific index
    /// </summary>
    public static void DeleteProfile(int index)
    {
        bool wasActiveProfile = (ProfileListManager.ActiveProfileIndex == index);
        ProfileListManager.DeleteProfile(index);
        
        // If this was the active profile, update tmp name based on new active profile
        if (wasActiveProfile)
        {
            int newActiveIndex = ProfileListManager.ActiveProfileIndex;
            if (newActiveIndex >= 0)
            {
                // Update tmp name to the newly selected profile
                tmpProfileName = ProfileListManager.GetProfileName(newActiveIndex);
            }
            else
            {
                // No active profile, clear tmp name
                tmpProfileName = "";
            }
        }
        
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Selects a profile at the given index
    /// </summary>
    public static void SelectProfile(int index)
    {
        if (ProfileListManager.SelectProfile(index))
        {
            tmpProfileName = ProfileListManager.GetProfileName(index);
        }
    }
    
    void Start()
    {
        // Initialize ProfileListManager cache
        ProfileListManager.InitializeCache();
        
        // Load active profile name if available
        string activeName = ProfileListManager.ActiveProfileName;
        if (!string.IsNullOrEmpty(activeName))
        {
            tmpProfileName = activeName;
        }
    }
}
