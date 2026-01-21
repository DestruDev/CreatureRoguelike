using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Centralized run save/load helpers (PlayerPrefs-backed).
/// </summary>
public class SaveRun : MonoBehaviour
{
    // PlayerPrefs keys (run-scoped)
    public const string MapKey = "Map";
    public const string CurrencyKey = "PlayerCurrency";

    // --------------------
    // Map
    // --------------------

    public static bool HasMap()
    {
        return PlayerPrefs.HasKey(MapKey);
    }

    public static void SaveMap(Map.Map map)
    {
        if (map == null) return;

        string json = JsonConvert.SerializeObject(
            map,
            Formatting.Indented,
            new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
        );

        PlayerPrefs.SetString(MapKey, json);
        PlayerPrefs.Save();
    }

    public static Map.Map LoadMap()
    {
        if (!HasMap()) return null;

        string mapJson = PlayerPrefs.GetString(MapKey);
        if (string.IsNullOrWhiteSpace(mapJson)) return null;

        return JsonConvert.DeserializeObject<Map.Map>(mapJson);
    }

    public static void DeleteMap()
    {
        if (!HasMap()) return;
        PlayerPrefs.DeleteKey(MapKey);
    }

    // --------------------
    // Currency
    // --------------------

    public static bool HasCurrency()
    {
        return PlayerPrefs.HasKey(CurrencyKey);
    }

    public static void SaveCurrency(int gold)
    {
        PlayerPrefs.SetInt(CurrencyKey, gold);
        PlayerPrefs.Save();
    }

    public static int LoadCurrency(int fallback = 0)
    {
        return HasCurrency() ? PlayerPrefs.GetInt(CurrencyKey) : fallback;
    }

    public static void DeleteCurrency()
    {
        if (!HasCurrency()) return;
        PlayerPrefs.DeleteKey(CurrencyKey);
    }

    // --------------------
    // Utility
    // --------------------

    public static void Commit()
    {
        PlayerPrefs.Save();
    }
}
