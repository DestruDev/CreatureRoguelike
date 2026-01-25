using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized run save/load helpers (PlayerPrefs-backed).
/// All run data is scoped by the active profile index to ensure each profile has separate runs.
/// </summary>
public class SaveRun : MonoBehaviour
{
    // Base PlayerPrefs keys (will be scoped by profile)
    private const string MapKeyBase = "Map";
    private const string CurrencyKeyBase = "PlayerCurrency";
    private const string ActiveBattleLevelIdKeyBase = "ActiveBattleLevelId";
    private const string ActiveBattleStageKeyBase = "ActiveBattleStage";
    private const string AllyHPKeyBase = "AllyHP";

    /// <summary>
    /// Gets a profile-scoped key by appending the active profile index to the base key.
    /// If no profile is active, uses a default profile index of 0 for backward compatibility.
    /// </summary>
    private static string GetProfileScopedKey(string baseKey)
    {
        int profileIndex = SaveProfiles.ActiveProfileIndex;
        // If no profile is active, use -1 as a fallback (will be converted to "Profile_-1")
        if (profileIndex < 0)
        {
            // For backward compatibility, if no profile exists, use unscoped key
            // This allows existing saves to still work
            return baseKey;
        }
        return $"Profile_{profileIndex}_{baseKey}";
    }

    // --------------------
    // Map
    // --------------------

    public static bool HasMap()
    {
        string key = GetProfileScopedKey(MapKeyBase);
        return PlayerPrefs.HasKey(key);
    }

    public static void SaveMap(Map.Map map)
    {
        if (map == null) return;

        string json = JsonConvert.SerializeObject(
            map,
            Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        );

        string key = GetProfileScopedKey(MapKeyBase);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static Map.Map LoadMap()
    {
        if (!HasMap()) return null;

        string key = GetProfileScopedKey(MapKeyBase);
        string mapJson = PlayerPrefs.GetString(key);
        if (string.IsNullOrWhiteSpace(mapJson)) return null;

        return JsonConvert.DeserializeObject<Map.Map>(mapJson);
    }

    public static void DeleteMap()
    {
        if (!HasMap()) return;
        string key = GetProfileScopedKey(MapKeyBase);
        PlayerPrefs.DeleteKey(key);
    }

    // --------------------
    // Currency
    // --------------------

    public static bool HasCurrency()
    {
        string key = GetProfileScopedKey(CurrencyKeyBase);
        return PlayerPrefs.HasKey(key);
    }

    public static void SaveCurrency(int gold)
    {
        string key = GetProfileScopedKey(CurrencyKeyBase);
        PlayerPrefs.SetInt(key, gold);
        PlayerPrefs.Save();
    }

    public static int LoadCurrency(int fallback = 0)
    {
        if (!HasCurrency()) return fallback;
        string key = GetProfileScopedKey(CurrencyKeyBase);
        return PlayerPrefs.GetInt(key, fallback);
    }

    public static void DeleteCurrency()
    {
        if (!HasCurrency()) return;
        string key = GetProfileScopedKey(CurrencyKeyBase);
        PlayerPrefs.DeleteKey(key);
    }

    // --------------------
    // Utility
    // --------------------

    public static void Commit()
    {
        PlayerPrefs.Save();
    }

    // --------------------
    // Active battle (resume at beginning of battle)
    // --------------------

    public static bool HasActiveBattle()
    {
        string key = GetProfileScopedKey(ActiveBattleLevelIdKeyBase);
        return PlayerPrefs.HasKey(key);
    }

    public static void SetActiveBattle(string levelId, int stage)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return;

        string levelIdKey = GetProfileScopedKey(ActiveBattleLevelIdKeyBase);
        string stageKey = GetProfileScopedKey(ActiveBattleStageKeyBase);
        PlayerPrefs.SetString(levelIdKey, levelId);
        PlayerPrefs.SetInt(stageKey, stage);
        PlayerPrefs.Save();
    }

    public static (string levelId, int stage) LoadActiveBattle()
    {
        if (!HasActiveBattle()) return (null, 0);
        string levelIdKey = GetProfileScopedKey(ActiveBattleLevelIdKeyBase);
        string stageKey = GetProfileScopedKey(ActiveBattleStageKeyBase);
        string levelId = PlayerPrefs.GetString(levelIdKey);
        int stage = PlayerPrefs.GetInt(stageKey, 0);
        return (levelId, stage);
    }

    public static void ClearActiveBattle()
    {
        string levelIdKey = GetProfileScopedKey(ActiveBattleLevelIdKeyBase);
        string stageKey = GetProfileScopedKey(ActiveBattleStageKeyBase);
        if (PlayerPrefs.HasKey(levelIdKey)) PlayerPrefs.DeleteKey(levelIdKey);
        if (PlayerPrefs.HasKey(stageKey)) PlayerPrefs.DeleteKey(stageKey);
    }

    // --------------------
    // Ally HP (persistent across floors/rounds)
    // --------------------

    public static bool HasAllyHP()
    {
        string key = GetProfileScopedKey(AllyHPKeyBase);
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>
    /// Saves ally HP data. Dictionary key is unitID, value is current HP.
    /// </summary>
    public static void SaveAllyHP(Dictionary<string, int> allyHPData)
    {
        if (allyHPData == null) return;

        string json = JsonConvert.SerializeObject(allyHPData, Formatting.None);
        string key = GetProfileScopedKey(AllyHPKeyBase);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads ally HP data. Returns dictionary with unitID as key and current HP as value.
    /// Returns empty dictionary if no save data exists.
    /// </summary>
    public static Dictionary<string, int> LoadAllyHP()
    {
        if (!HasAllyHP()) return new Dictionary<string, int>();

        string key = GetProfileScopedKey(AllyHPKeyBase);
        string json = PlayerPrefs.GetString(key);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, int>();

        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
        }
        catch
        {
            Debug.LogWarning("Failed to deserialize ally HP data. Returning empty dictionary.");
            return new Dictionary<string, int>();
        }
    }

    /// <summary>
    /// Saves a single ally's HP by unitID
    /// </summary>
    public static void SaveAllyHP(string unitID, int currentHP)
    {
        if (string.IsNullOrEmpty(unitID)) return;

        Dictionary<string, int> allyHPData = LoadAllyHP();
        allyHPData[unitID] = currentHP;
        SaveAllyHP(allyHPData);
    }

    /// <summary>
    /// Loads a single ally's HP by unitID. Returns -1 if not found.
    /// </summary>
    public static int LoadAllyHP(string unitID)
    {
        if (string.IsNullOrEmpty(unitID)) return -1;

        Dictionary<string, int> allyHPData = LoadAllyHP();
        return allyHPData.TryGetValue(unitID, out int hp) ? hp : -1;
    }

    /// <summary>
    /// Deletes all saved ally HP data
    /// </summary>
    public static void DeleteAllyHP()
    {
        if (!HasAllyHP()) return;
        string key = GetProfileScopedKey(AllyHPKeyBase);
        PlayerPrefs.DeleteKey(key);
    }
}
