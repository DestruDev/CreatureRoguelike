using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
    
    [Header("Enemy Unit Data (ScriptableObjects) - B1-1")]
    [Tooltip("For each enemy spawn slot, assign multiple possible enemy unit data ScriptableObjects.")]
    public EnemySpawnSlot[] enemySpawnSlotsB1_1 = new EnemySpawnSlot[3];
    
    [Header("Enemy Unit Data (ScriptableObjects) - B1-2")]
    [Tooltip("For each enemy spawn slot, assign multiple possible enemy unit data ScriptableObjects.")]
    public EnemySpawnSlot[] enemySpawnSlotsB1_2 = new EnemySpawnSlot[3];
    
    [Header("Enemy Unit Data (ScriptableObjects) - B1-3")]
    [Tooltip("For each enemy spawn slot, assign multiple possible enemy unit data ScriptableObjects.")]
    public EnemySpawnSlot[] enemySpawnSlotsB1_3 = new EnemySpawnSlot[3];
    
    [Header("Enemy Spawn Settings")]
    [Tooltip("If enabled, randomly selects from all assigned enemies for each slot. If disabled, uses the first assigned enemy for each slot.")]
    public bool useRandomEnemySpawns = false;
    
    [Header("Unit Scale Settings")]
    [Tooltip("Scale multiplier for spawned allies/creatures. Set to 1.0 for normal size, higher values make sprites bigger.")]
    public float allyScale = 1.0f;
    
    [Tooltip("Scale multiplier for spawned enemies. Set to 1.0 for normal size, higher values make sprites bigger.")]
    public float enemyScale = 1.0f;
    
    
    void Start()
    {
        SpawnAllUnits();
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
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[i].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(creatureUnitData[i], i);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(creatureUnitData[i], creatureSpawnAreas[i], unitName, allyScale);
    
				// Ensure unit is set as player unit (spawn area determines team)
				Unit unit = unitObj.GetComponent<Unit>();
				if (unit != null)
				{
					unit.SetTeamAssignment(true); // Force player unit based on spawn area
				}
                
                // Debug.Log("Spawned creature " + (i + 1) + " (" + unitName + ") at " + creatureSpawnAreas[i].name);
            }
        }
        
        // Spawn enemies
        for (int i = 0; i < enemySpawnSlotsB1_1.Length && i < enemySpawnAreas.Length; i++)
        {
			if (enemySpawnAreas[i] != null && enemySpawnSlotsB1_1[i] != null)
            {
                // Get an enemy from the possible enemies for this slot (random or first based on setting)
                CreatureUnitData selectedEnemy = enemySpawnSlotsB1_1[i].GetEnemy(useRandomEnemySpawns);
                
                if (selectedEnemy == null)
                {
                    Debug.LogWarning($"No valid enemy unit data assigned for enemy spawn slot {i + 1}. Skipping spawn.");
                    continue;
                }
                
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[i].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(selectedEnemy, i);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(selectedEnemy, enemySpawnAreas[i], unitName, enemyScale);
    
				// Ensure unit is set as enemy unit (spawn area determines team, not ScriptableObject)
				Unit unit = unitObj.GetComponent<Unit>();
				if (unit != null)
				{
					unit.SetTeamAssignment(false); // Force enemy unit based on spawn area
				}
                
                // Debug.Log("Spawned enemy " + (i + 1) + " (" + unitName + ") at " + enemySpawnAreas[i].name);
            }
        }
    }
    
	public void ClearAllSpawnedUnits()
    {
		// Clear creature spawn areas
		for (int i = 0; i < creatureSpawnAreas.Length; i++)
		{
			if (creatureSpawnAreas[i] == null) continue;
			for (int c = creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
			{
				DestroyImmediate(creatureSpawnAreas[i].GetChild(c).gameObject);
			}
		}
		
		// Clear enemy spawn areas
		for (int i = 0; i < enemySpawnAreas.Length; i++)
		{
			if (enemySpawnAreas[i] == null) continue;
			for (int c = enemySpawnAreas[i].childCount - 1; c >= 0; c--)
			{
				DestroyImmediate(enemySpawnAreas[i].GetChild(c).gameObject);
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
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[index].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(creatureUnitData[index], index);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(creatureUnitData[index], creatureSpawnAreas[index], unitName, allyScale);
    
				// Ensure unit is set as player unit (spawn area determines team)
				Unit unit = unitObj.GetComponent<Unit>();
				if (unit != null)
				{
					unit.SetTeamAssignment(true);
				}
                
                Debug.Log("Respawned creature " + (index + 1) + " (" + unitName + ")");
            }
        }
    }
    
	/// <summary>
	/// Respawns an enemy at a specific spawn area index (0-2)
	/// </summary>
	public void RespawnEnemy(int index)
    {
		if (index >= 0 && index < enemySpawnSlotsB1_1.Length && index < enemySpawnAreas.Length)
        {
			if (enemySpawnAreas[index] != null && enemySpawnSlotsB1_1[index] != null)
            {
                // Get an enemy from the possible enemies for this slot (random or first based on setting)
                CreatureUnitData selectedEnemy = enemySpawnSlotsB1_1[index].GetEnemy(useRandomEnemySpawns);
                
                if (selectedEnemy == null)
                {
                    Debug.LogWarning($"No valid enemy unit data assigned for enemy spawn slot {index + 1}. Cannot respawn.");
                    return;
                }
                
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[index].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(selectedEnemy, index);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(selectedEnemy, enemySpawnAreas[index], unitName, enemyScale);
    
				// Ensure unit is set as enemy unit (spawn area determines team)
				Unit unit = unitObj.GetComponent<Unit>();
				if (unit != null)
				{
					unit.SetTeamAssignment(false);
				}
                
                Debug.Log("Respawned enemy " + (index + 1) + " (" + unitName + ")");
            }
        }
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
