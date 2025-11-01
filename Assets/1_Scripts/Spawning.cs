using UnityEngine;
using UnityEngine.UI;

public class Spawning : MonoBehaviour
{
    [Header("Spawn Areas")]
    public Transform[] creatureSpawnAreas = new Transform[3];
    public Transform[] enemySpawnAreas = new Transform[3];
    
    [Header("Unit Data (ScriptableObjects)")]
	public CreatureUnitData[] creatureData = new CreatureUnitData[3];
	public EnemyUnitData[] enemyData = new EnemyUnitData[3];
    
    
    void Start()
    {
        SpawnAllUnits();
    }
    
    public void SpawnAllUnits()
    {
        SpawnCreatures();
        SpawnEnemies();
        
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
    
	public void SpawnCreatures()
    {
		for (int i = 0; i < creatureData.Length && i < creatureSpawnAreas.Length; i++)
        {
			if (creatureData[i] != null && creatureSpawnAreas[i] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[i].GetChild(c).gameObject);
				}
                
				// Create unit from ScriptableObject data, using unitName from ScriptableObject
				string creatureName = !string.IsNullOrEmpty(creatureData[i].unitName) ? creatureData[i].unitName : "Creature_" + (i + 1);
				var creature = CreateUnitFromData(creatureData[i], UnitType.Creature, creatureSpawnAreas[i], creatureName);
                
                Debug.Log("Spawned creature " + (i + 1) + " (" + creatureName + ") at " + creatureSpawnAreas[i].name);
            }
        }
    }
    
	public void SpawnEnemies()
    {
		for (int i = 0; i < enemyData.Length && i < enemySpawnAreas.Length; i++)
        {
			if (enemyData[i] != null && enemySpawnAreas[i] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[i].GetChild(c).gameObject);
				}
                
				// Create unit from ScriptableObject data, using unitName from ScriptableObject
				string enemyName = !string.IsNullOrEmpty(enemyData[i].unitName) ? enemyData[i].unitName : "Enemy_" + (i + 1);
				var enemy = CreateUnitFromData(enemyData[i], UnitType.Enemy, enemySpawnAreas[i], enemyName);
                
                Debug.Log("Spawned enemy " + (i + 1) + " (" + enemyName + ") at " + enemySpawnAreas[i].name);
            }
        }
    }
    
	public void ClearAllSpawnedUnits()
    {
		// Clear creatures
		for (int i = 0; i < creatureSpawnAreas.Length; i++)
		{
			if (creatureSpawnAreas[i] == null) continue;
			for (int c = creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
			{
				DestroyImmediate(creatureSpawnAreas[i].GetChild(c).gameObject);
			}
		}
		
		// Clear enemies
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
    
	// Method to respawn a specific creature
	public void RespawnCreature(int index)
    {
		if (index >= 0 && index < creatureData.Length && index < creatureSpawnAreas.Length)
        {
			if (creatureData[index] != null && creatureSpawnAreas[index] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[index].GetChild(c).gameObject);
				}
                
				// Create unit from ScriptableObject data, using unitName from ScriptableObject
				string creatureName = !string.IsNullOrEmpty(creatureData[index].unitName) ? creatureData[index].unitName : "Creature_" + (index + 1);
				var creature = CreateUnitFromData(creatureData[index], UnitType.Creature, creatureSpawnAreas[index], creatureName);
                
                Debug.Log("Respawned creature " + (index + 1) + " (" + creatureName + ")");
            }
        }
    }
    
    // Method to respawn a specific enemy
	public void RespawnEnemy(int index)
    {
		if (index >= 0 && index < enemyData.Length && index < enemySpawnAreas.Length)
        {
			if (enemyData[index] != null && enemySpawnAreas[index] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[index].GetChild(c).gameObject);
				}
                
				// Create unit from ScriptableObject data, using unitName from ScriptableObject
				string enemyName = !string.IsNullOrEmpty(enemyData[index].unitName) ? enemyData[index].unitName : "Enemy_" + (index + 1);
				var enemy = CreateUnitFromData(enemyData[index], UnitType.Enemy, enemySpawnAreas[index], enemyName);
                
                Debug.Log("Respawned enemy " + (index + 1) + " (" + enemyName + ")");
            }
        }
    }
    
    /// <summary>
    /// Creates a unit GameObject from ScriptableObject data
    /// </summary>
    private GameObject CreateUnitFromData(ScriptableObject unitData, UnitType unitType, Transform parent, string unitName)
    {
        // Create the main GameObject
        GameObject unitObj = new GameObject(unitName);
        unitObj.transform.SetParent(parent);
        unitObj.transform.localPosition = Vector3.zero;
        unitObj.transform.localRotation = Quaternion.identity;
        unitObj.transform.localScale = Vector3.one;
        
        // Add SpriteRenderer component
        SpriteRenderer spriteRenderer = unitObj.AddComponent<SpriteRenderer>();
        
        // Add Unit component
        Unit unit = unitObj.AddComponent<Unit>();
        
        // Initialize unit with data (this will set sprite and color via InitializeUnit)
        SetUnitData(unit, unitData, unitType);
        
        return unitObj;
    }
    
    /// <summary>
    /// Sets the unit data on the Unit component using public initialization method
    /// </summary>
    private void SetUnitData(Unit unit, ScriptableObject data, UnitType unitType)
    {
        if (unitType == UnitType.Creature && data is CreatureUnitData creatureData)
        {
            unit.InitializeWithData(UnitType.Creature, creatureData, null);
        }
        else if (unitType == UnitType.Enemy && data is EnemyUnitData enemyData)
        {
            unit.InitializeWithData(UnitType.Enemy, null, enemyData);
        }
    }
}
