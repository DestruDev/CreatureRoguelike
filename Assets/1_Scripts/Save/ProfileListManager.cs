using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages a list of up to 5 profile slots. Handles adding, deleting, and selecting profiles.
/// </summary>
public class ProfileListManager : MonoBehaviour
{
    private const int MAX_PROFILES = 5;
    private const string ACTIVE_PROFILE_INDEX_KEY = "ActiveProfileIndex";
    private const string PROFILE_LIST_COUNT_KEY = "ProfileListCount";
    private const string PROFILE_NAME_PREFIX = "ProfileName_";
    
    // Cache of profile names for quick access
    private static List<string> profileCache = new List<string>();
    private static bool cacheInitialized = false;
    
    /// <summary>
    /// Gets the maximum number of profiles allowed
    /// </summary>
    public static int MaxProfiles => MAX_PROFILES;
    
    /// <summary>
    /// Gets the currently active profile index (-1 if none selected)
    /// </summary>
    public static int ActiveProfileIndex
    {
        get
        {
            return PlayerPrefs.GetInt(ACTIVE_PROFILE_INDEX_KEY, -1);
        }
        private set
        {
            PlayerPrefs.SetInt(ACTIVE_PROFILE_INDEX_KEY, value);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Gets the name of the currently active profile, or empty string if none
    /// </summary>
    public static string ActiveProfileName
    {
        get
        {
            int index = ActiveProfileIndex;
            if (index >= 0 && index < MAX_PROFILES)
            {
                return GetProfileName(index);
            }
            return "";
        }
    }
    
    void Start()
    {
        InitializeCache();
    }
    
    /// <summary>
    /// Initializes the profile cache from PlayerPrefs
    /// </summary>
    public static void InitializeCache()
    {
        if (cacheInitialized) return;
        
        profileCache.Clear();
        int count = PlayerPrefs.GetInt(PROFILE_LIST_COUNT_KEY, 0);
        
        for (int i = 0; i < MAX_PROFILES; i++)
        {
            string key = PROFILE_NAME_PREFIX + i;
            if (PlayerPrefs.HasKey(key))
            {
                string name = PlayerPrefs.GetString(key, "");
                if (!string.IsNullOrEmpty(name))
                {
                    profileCache.Add(name);
                }
                else
                {
                    profileCache.Add(null);
                }
            }
            else
            {
                profileCache.Add(null);
            }
        }
        
        cacheInitialized = true;
    }
    
    /// <summary>
    /// Gets all profile names (non-null entries)
    /// </summary>
    public static List<string> GetProfiles()
    {
        InitializeCache();
        return profileCache.Where(p => p != null).ToList();
    }
    
    /// <summary>
    /// Gets the profile name at a specific index
    /// </summary>
    public static string GetProfileName(int index)
    {
        if (index < 0 || index >= MAX_PROFILES) return "";
        
        InitializeCache();
        return profileCache[index] ?? "";
    }
    
    /// <summary>
    /// Checks if a profile exists at the given index
    /// </summary>
    public static bool HasProfileAt(int index)
    {
        if (index < 0 || index >= MAX_PROFILES) return false;
        
        InitializeCache();
        return profileCache[index] != null;
    }
    
    /// <summary>
    /// Gets the first available slot index, or -1 if all slots are full
    /// </summary>
    public static int GetFirstAvailableSlot()
    {
        InitializeCache();
        
        for (int i = 0; i < MAX_PROFILES; i++)
        {
            if (profileCache[i] == null)
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Adds a new profile with the given name. Returns the slot index if successful, -1 if failed (all slots full or name is empty)
    /// </summary>
    public static int AddProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("ProfileListManager: Cannot add profile with empty name");
            return -1;
        }
        
        InitializeCache();
        
        // Check if we're at max capacity
        int availableSlot = GetFirstAvailableSlot();
        if (availableSlot == -1)
        {
            Debug.LogWarning("ProfileListManager: Cannot add profile - all 5 slots are full");
            return -1;
        }
        
        // Add the profile (duplicate names are allowed)
        profileCache[availableSlot] = name;
        string key = PROFILE_NAME_PREFIX + availableSlot;
        PlayerPrefs.SetString(key, name);
        
        // Update count
        int currentCount = profileCache.Count(p => p != null);
        PlayerPrefs.SetInt(PROFILE_LIST_COUNT_KEY, currentCount);
        PlayerPrefs.Save();
        
        // Automatically select the newly created profile
        SelectProfile(availableSlot);
        
        return availableSlot;
    }
    
    /// <summary>
    /// Adds a new profile with the given name at a specific index. Returns true if successful, false if failed.
    /// </summary>
    public static bool AddProfileAt(int index, string name)
    {
        if (index < 0 || index >= MAX_PROFILES)
        {
            Debug.LogWarning($"ProfileListManager: Invalid profile index {index}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("ProfileListManager: Cannot add profile with empty name");
            return false;
        }
        
        InitializeCache();
        
        // Check if slot is already occupied
        if (profileCache[index] != null)
        {
            Debug.LogWarning($"ProfileListManager: Profile slot {index} is already occupied");
            return false;
        }
        
        // Add the profile at the specified index (duplicate names are allowed)
        profileCache[index] = name;
        string key = PROFILE_NAME_PREFIX + index;
        PlayerPrefs.SetString(key, name);
        
        // Update count
        int currentCount = profileCache.Count(p => p != null);
        PlayerPrefs.SetInt(PROFILE_LIST_COUNT_KEY, currentCount);
        PlayerPrefs.Save();
        
        // Automatically select the newly created profile
        SelectProfile(index);
        
        return true;
    }
    
    /// <summary>
    /// Deletes a profile at the given index
    /// </summary>
    public static bool DeleteProfile(int index)
    {
        if (index < 0 || index >= MAX_PROFILES)
        {
            Debug.LogWarning($"ProfileListManager: Invalid profile index {index}");
            return false;
        }
        
        InitializeCache();
        
        if (profileCache[index] == null)
        {
            Debug.LogWarning($"ProfileListManager: No profile exists at index {index}");
            return false;
        }
        
        // Delete the profile
        profileCache[index] = null;
        string key = PROFILE_NAME_PREFIX + index;
        PlayerPrefs.DeleteKey(key);
        
        // Update count
        int currentCount = profileCache.Count(p => p != null);
        PlayerPrefs.SetInt(PROFILE_LIST_COUNT_KEY, currentCount);
        
        // If this was the active profile, find the next available profile by index (highest first)
        if (ActiveProfileIndex == index)
        {
            int newActiveIndex = -1;
            // Find the first available profile by index number (prioritized by highest index)
            for (int i = MAX_PROFILES - 1; i >= 0; i--)
            {
                if (i != index && profileCache[i] != null)
                {
                    newActiveIndex = i;
                    break;
                }
            }
            
            ActiveProfileIndex = newActiveIndex;
        }
        
        PlayerPrefs.Save();
        
        return true;
    }
    
    /// <summary>
    /// Selects a profile at the given index (makes it active)
    /// </summary>
    public static bool SelectProfile(int index)
    {
        if (index < 0 || index >= MAX_PROFILES)
        {
            Debug.LogWarning($"ProfileListManager: Invalid profile index {index}");
            return false;
        }
        
        InitializeCache();
        
        if (profileCache[index] == null)
        {
            Debug.LogWarning($"ProfileListManager: No profile exists at index {index}");
            return false;
        }
        
        ActiveProfileIndex = index;
        return true;
    }
    
    /// <summary>
    /// Gets the number of profiles currently stored
    /// </summary>
    public static int GetProfileCount()
    {
        InitializeCache();
        return profileCache.Count(p => p != null);
    }
    
    /// <summary>
    /// Checks if all profile slots are full
    /// </summary>
    public static bool IsFull()
    {
        return GetProfileCount() >= MAX_PROFILES;
    }
    
    /// <summary>
    /// Clears the cache (useful for testing or when manually modifying PlayerPrefs)
    /// </summary>
    public static void ClearCache()
    {
        cacheInitialized = false;
        profileCache.Clear();
    }
}
