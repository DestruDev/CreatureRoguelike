using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class EnemySpawnSlot
{
    [Tooltip("List of possible enemy unit data ScriptableObjects for this spawn slot. If random spawning is enabled, a random one will be selected. Otherwise, the first one will be used.")]
    public CreatureUnitData[] possibleEnemies = new CreatureUnitData[0];
    
    /// <summary>
    /// Gets an enemy from the possible enemies list. If useRandom is true, returns a random one; otherwise returns the first valid one.
    /// Returns null if the list is empty.
    /// </summary>
    public CreatureUnitData GetEnemy(bool useRandom)
    {
        if (possibleEnemies == null || possibleEnemies.Length == 0)
            return null;
        
        // Filter out null entries
        List<CreatureUnitData> validEnemies = new List<CreatureUnitData>();
        foreach (var enemy in possibleEnemies)
        {
            if (enemy != null)
                validEnemies.Add(enemy);
        }
        
        if (validEnemies.Count == 0)
            return null;
        
        // Return random enemy or first enemy based on useRandom flag
        if (useRandom)
        {
            return validEnemies[Random.Range(0, validEnemies.Count)];
        }
        else
        {
            return validEnemies[0]; // Return first valid enemy
        }
    }
}

public class Spawning : MonoBehaviour
{
    [Header("Creature Spawn Areas")]
    [Tooltip("Spawn areas for creature units (player units)")]
    public Transform[] creatureSpawnAreas = new Transform[3];
    
    [Header("Creature Unit Data (ScriptableObjects)")]
    [Tooltip("Unit data for each creature spawn area. Should be CreatureUnitData.")]
    public CreatureUnitData[] creatureUnitData = new CreatureUnitData[3];
    
    [Header("Enemy Spawn Areas")]
    [Tooltip("Spawn areas for enemy units")]
    public Transform[] enemySpawnAreas = new Transform[3];
    
    [Header("Unit Scale Settings")]
    [Tooltip("Scale multiplier for spawned allies/creatures. Set to 1.0 for normal size, higher values make sprites bigger.")]
    public float allyScale = 1.0f;
    
    [Tooltip("Scale multiplier for spawned enemies. Set to 1.0 for normal size, higher values make sprites bigger.")]
    public float enemyScale = 1.0f;
    
    
    void Start()
    {
        // Don't spawn creatures on Start - they will be spawned when a level is selected from the map
        // This ensures allies and enemies appear at the same time
        // SpawnCreaturesOnly(); // Commented out - creatures now spawn with enemies when level starts
    }
    
    /// <summary>
    /// Spawns only creatures (player units), not enemies
    /// </summary>
    public void SpawnCreaturesOnly()
    {
        // Spawn creatures at each index
        for (int i = 0; i < creatureUnitData.Length && i < creatureSpawnAreas.Length; i++)
        {
            if (creatureUnitData[i] != null && creatureSpawnAreas[i] != null)
            {
                SpawnCreatureAtIndex(i);
            }
        }
    }
    
    public void SpawnAllUnits()
    {
        SpawnUnits();
        
        // Notify GameManager to update UI after all units are spawned
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // Delay slightly to ensure all units are fully initialized
            StartCoroutine(DelayedUIUpdate(gameManager));
        }
    }
    
    private System.Collections.IEnumerator DelayedUIUpdate(GameManager gameManager)
    {
        yield return new WaitForEndOfFrame();
        gameManager.UpdateAllUnitUI();
    }
    
	/// <summary>
	/// Spawns all units from the unit data arrays
	/// </summary>
	public void SpawnUnits()
    {
		// Spawn creatures
		for (int i = 0; i < creatureUnitData.Length && i < creatureSpawnAreas.Length; i++)
        {
			if (creatureUnitData[i] != null && creatureSpawnAreas[i] != null)
            {
                SpawnCreatureAtIndex(i);
            }
        }
        
        // Spawn enemies for current level (defaults to B1-1)
        LevelData defaultLevelData = GetCurrentLevelDataWithFallback();
        
        if (defaultLevelData != null)
        {
            SpawnEnemiesForLevel(defaultLevelData);
        }
        else
        {
            Debug.LogWarning("Could not find LevelData for B1-1. Enemy spawning skipped.");
        }
    }
    
    /// <summary>
    /// Finds a LevelData ScriptableObject by its levelID
    /// </summary>
    private LevelData FindLevelDataByID(string levelID)
    {
        if (string.IsNullOrEmpty(levelID))
            return null;
        
#if UNITY_EDITOR
        // In editor, use AssetDatabase for faster lookup
        string[] guids = AssetDatabase.FindAssets("t:LevelData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (levelData != null && levelData.levelID == levelID)
            {
                return levelData;
            }
        }
#else
        // At runtime, use Resources (requires LevelData assets to be in a Resources folder)
        LevelData[] allLevels = Resources.LoadAll<LevelData>("");
        foreach (LevelData levelData in allLevels)
        {
            if (levelData != null && levelData.levelID == levelID)
            {
                return levelData;
            }
        }
#endif
        return null;
    }
    
    /// <summary>
    /// Gets current level data from LevelNavigation, with fallback to B1-1
    /// </summary>
    private LevelData GetCurrentLevelDataWithFallback()
    {
        // Try to get current level from LevelNavigation
        LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
        LevelData currentLevelData = null;
        
        if (levelNavigation != null)
        {
            string currentLevelID = levelNavigation.GetCurrentLevel();
            currentLevelData = FindLevelDataByID(currentLevelID);
        }
        
        // Fallback to B1-1 if no current level found
        if (currentLevelData == null)
        {
            currentLevelData = FindLevelDataByID("B1-1");
        }
        
        return currentLevelData;
    }
    
    /// <summary>
    /// Spawns enemies for a specific level by level ID string
    /// </summary>
    public void SpawnEnemiesForLevel(string levelID)
    {
        LevelData levelData = FindLevelDataByID(levelID);
        
        if (levelData != null)
        {
            SpawnEnemiesForLevel(levelData);
        }
        else
        {
            Debug.LogWarning($"Could not find LevelData for level ID: {levelID}");
        }
    }
    
    /// <summary>
    /// Spawns enemies for a specific level using LevelData
    /// </summary>
    public void SpawnEnemiesForLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogWarning("LevelData is null. Cannot spawn enemies.");
            return;
        }
        
        if (levelData.enemySpawnSlots == null || levelData.enemySpawnSlots.Length == 0)
        {
            Debug.LogWarning($"No enemy spawn slots configured in LevelData for level {levelData.levelID}.");
            return;
        }
        
        // Spawn enemies at each index
        for (int i = 0; i < levelData.enemySpawnSlots.Length && i < enemySpawnAreas.Length; i++)
        {
			if (enemySpawnAreas[i] != null && levelData.enemySpawnSlots[i] != null)
            {
                SpawnEnemyAtIndex(i, levelData);
            }
        }
    }
    
	public void ClearAllSpawnedUnits()
    {
		// Clear creature spawn areas
		for (int i = 0; i < creatureSpawnAreas.Length; i++)
		{
			if (creatureSpawnAreas[i] != null)
            {
                ClearSpawnArea(creatureSpawnAreas[i]);
            }
		}
		
		// Clear enemy spawn areas
		for (int i = 0; i < enemySpawnAreas.Length; i++)
		{
			if (enemySpawnAreas[i] != null)
            {
                ClearSpawnArea(enemySpawnAreas[i]);
            }
		}
        
        Debug.Log("Cleared all spawned units");
    }
    
	/// <summary>
	/// Respawns a creature at a specific spawn area index (0-2)
	/// </summary>
	public void RespawnCreature(int index)
    {
		if (index >= 0 && index < creatureUnitData.Length && index < creatureSpawnAreas.Length)
        {
			if (creatureUnitData[index] != null && creatureSpawnAreas[index] != null)
            {
                SpawnCreatureAtIndex(index);
                Debug.Log("Respawned creature " + (index + 1));
            }
        }
    }
    
	/// <summary>
	/// Respawns an enemy at a specific spawn area index (0-2)
	/// Uses the current level from LevelNavigation if available
	/// </summary>
	public void RespawnEnemy(int index)
    {
        // Get current level data with fallback to B1-1
        LevelData currentLevelData = GetCurrentLevelDataWithFallback();
        
        if (currentLevelData == null || currentLevelData.enemySpawnSlots == null)
        {
            Debug.LogWarning($"No LevelData found for respawning enemy at index {index}.");
            return;
        }
        
		if (index >= 0 && index < currentLevelData.enemySpawnSlots.Length && index < enemySpawnAreas.Length)
        {
			if (enemySpawnAreas[index] != null && currentLevelData.enemySpawnSlots[index] != null)
            {
                SpawnEnemyAtIndex(index, currentLevelData);
                Debug.Log("Respawned enemy " + (index + 1));
            }
        }
    }
    
    /// <summary>
    /// Clears all children under a spawn area
    /// </summary>
    private void ClearSpawnArea(Transform spawnArea)
    {
        if (spawnArea == null)
            return;
            
        for (int c = spawnArea.childCount - 1; c >= 0; c--)
        {
            DestroyImmediate(spawnArea.GetChild(c).gameObject);
        }
    }
    
    /// <summary>
    /// Spawns a creature at the specified index
    /// </summary>
    private void SpawnCreatureAtIndex(int index)
    {
        if (index < 0 || index >= creatureUnitData.Length || index >= creatureSpawnAreas.Length)
            return;
            
        if (creatureUnitData[index] == null || creatureSpawnAreas[index] == null)
            return;
        
        // Clear existing children under this spawn area
        ClearSpawnArea(creatureSpawnAreas[index]);
        
        string unitName = GetUnitName(creatureUnitData[index], index);
        
        // Create unit from ScriptableObject data
        var unitObj = CreateUnitFromData(creatureUnitData[index], creatureSpawnAreas[index], unitName, allyScale);

        // Ensure unit is set as player unit (spawn area determines team)
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetTeamAssignment(true); // Force player unit based on spawn area
        }
        
        // Debug.Log("Spawned creature " + (index + 1) + " (" + unitName + ") at " + creatureSpawnAreas[index].name);
    }
    
    /// <summary>
    /// Spawns an enemy at the specified index using the provided LevelData
    /// </summary>
    private void SpawnEnemyAtIndex(int index, LevelData levelData)
    {
        if (index < 0 || index >= enemySpawnAreas.Length)
            return;
            
        if (enemySpawnAreas[index] == null || levelData == null || levelData.enemySpawnSlots == null)
            return;
            
        if (index >= levelData.enemySpawnSlots.Length || levelData.enemySpawnSlots[index] == null)
            return;
        
        // Get an enemy from the possible enemies for this slot (random or first based on levelData setting)
        CreatureUnitData selectedEnemy = levelData.enemySpawnSlots[index].GetEnemy(levelData.useRandomEnemySpawns);
        
        if (selectedEnemy == null)
        {
            Debug.LogWarning($"No valid enemy unit data assigned for enemy spawn slot {index + 1} in level {levelData.levelID}. Skipping spawn.");
            return;
        }
        
        // Clear existing children under this spawn area
        ClearSpawnArea(enemySpawnAreas[index]);
        
        string unitName = GetUnitName(selectedEnemy, index);
        
        // Create unit from ScriptableObject data
        var unitObj = CreateUnitFromData(selectedEnemy, enemySpawnAreas[index], unitName, enemyScale);

        // Ensure unit is set as enemy unit (spawn area determines team, not ScriptableObject)
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit != null)
        {
            unit.SetTeamAssignment(false); // Force enemy unit based on spawn area
        }
        
        Debug.Log($"Spawned enemy {index + 1} ({unitName}) for level {levelData.levelID} at {enemySpawnAreas[index].name}");
    }
    
    /// <summary>
    /// Creates a unit GameObject from ScriptableObject data
    /// </summary>
    private GameObject CreateUnitFromData(CreatureUnitData unitData, Transform parent, string unitName, float scale = 1.0f)
    {
        // Create the main GameObject
        GameObject unitObj = new GameObject(unitName);
        unitObj.transform.SetParent(parent);
        unitObj.transform.localPosition = Vector3.zero;
        unitObj.transform.localRotation = Quaternion.identity;
        unitObj.transform.localScale = Vector3.one * scale;
        
        // Add SpriteRenderer component
        SpriteRenderer spriteRenderer = unitObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Units";
        
        // Add Unit component
        Unit unit = unitObj.AddComponent<Unit>();
        
        // Initialize unit with data (this will set sprite and color via InitializeUnit)
        unit.InitializeWithData(unitData);
        
        // Add HighlightUnit component for turn-based highlighting
        unitObj.AddComponent<HighlightUnit>();
        
        // Add 2D collider for mouse selection (size it to match the sprite)
        BoxCollider2D collider = unitObj.AddComponent<BoxCollider2D>();
        if (spriteRenderer.sprite != null)
        {
            // Size the collider to match the sprite bounds
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            collider.size = spriteBounds.size;
        }
        else
        {
            // Default size if sprite isn't set yet
            collider.size = Vector2.one;
        }
        
        // Add Animator component if animator controller is assigned
        if (unitData.animatorController != null)
        {
            Animator animator = unitObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = unitData.animatorController;
            
            // Add UnitAnimations component to handle animations
            unitObj.AddComponent<UnitAnimations>();
        }
        
        return unitObj;
    }
    
    /// <summary>
    /// Gets the unit name from ScriptableObject data, with fallback
    /// </summary>
    private string GetUnitName(CreatureUnitData data, int index)
    {
        return !string.IsNullOrEmpty(data.unitName) ? data.unitName : "Creature_" + (index + 1);
    }
    
}
