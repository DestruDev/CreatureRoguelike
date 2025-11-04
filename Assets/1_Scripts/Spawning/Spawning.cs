using UnityEngine;
using UnityEngine.UI;

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
    
    [Header("Enemy Unit Data (ScriptableObjects)")]
    [Tooltip("Unit data for each enemy spawn area. Uses CreatureUnitData.")]
    public CreatureUnitData[] enemyUnitData = new CreatureUnitData[3];
    
    
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
				var unitObj = CreateUnitFromData(creatureUnitData[i], creatureSpawnAreas[i], unitName);
    
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
        for (int i = 0; i < enemyUnitData.Length && i < enemySpawnAreas.Length; i++)
        {
			if (enemyUnitData[i] != null && enemySpawnAreas[i] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[i].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(enemyUnitData[i], i);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(enemyUnitData[i], enemySpawnAreas[i], unitName);
    
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
				var unitObj = CreateUnitFromData(creatureUnitData[index], creatureSpawnAreas[index], unitName);
    
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
		if (index >= 0 && index < enemyUnitData.Length && index < enemySpawnAreas.Length)
        {
			if (enemyUnitData[index] != null && enemySpawnAreas[index] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[index].GetChild(c).gameObject);
				}
                
				string unitName = GetUnitName(enemyUnitData[index], index);
				
				// Create unit from ScriptableObject data
				var unitObj = CreateUnitFromData(enemyUnitData[index], enemySpawnAreas[index], unitName);
    
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
    private GameObject CreateUnitFromData(CreatureUnitData unitData, Transform parent, string unitName)
    {
        // Create the main GameObject
        GameObject unitObj = new GameObject(unitName);
        unitObj.transform.SetParent(parent);
        unitObj.transform.localPosition = Vector3.zero;
        unitObj.transform.localRotation = Quaternion.identity;
        unitObj.transform.localScale = Vector3.one;
        
        // Add SpriteRenderer component
        SpriteRenderer spriteRenderer = unitObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Units";
        
        // Add Unit component
        Unit unit = unitObj.AddComponent<Unit>();
        
        // Initialize unit with data (this will set sprite and color via InitializeUnit)
        unit.InitializeWithData(unitData);
        
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
