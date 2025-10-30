using UnityEngine;

public class Spawning : MonoBehaviour
{
    [Header("Spawn Areas")]
    public Transform[] creatureSpawnAreas = new Transform[3];
    public Transform[] enemySpawnAreas = new Transform[3];
    
    [Header("Unit Prefabs")]
	public GameObject[] creaturePrefabs = new GameObject[3];
	public GameObject[] enemyPrefabs = new GameObject[3];
    
    void Start()
    {
        SpawnAllUnits();
    }
    
    public void SpawnAllUnits()
    {
        SpawnCreatures();
        SpawnEnemies();
    }
    
	public void SpawnCreatures()
    {
		for (int i = 0; i < creaturePrefabs.Length && i < creatureSpawnAreas.Length; i++)
        {
			if (creaturePrefabs[i] != null && creatureSpawnAreas[i] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[i].GetChild(c).gameObject);
				}
                
				// Spawn new creature as child
				var creature = Instantiate(creaturePrefabs[i], creatureSpawnAreas[i].position, creatureSpawnAreas[i].rotation, creatureSpawnAreas[i]);
				creature.name = "Creature_" + (i + 1);
                
                Debug.Log("Spawned creature " + (i + 1) + " at " + creatureSpawnAreas[i].name);
            }
        }
    }
    
	public void SpawnEnemies()
    {
		for (int i = 0; i < enemyPrefabs.Length && i < enemySpawnAreas.Length; i++)
        {
			if (enemyPrefabs[i] != null && enemySpawnAreas[i] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[i].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[i].GetChild(c).gameObject);
				}
                
				// Spawn new enemy as child
				var enemy = Instantiate(enemyPrefabs[i], enemySpawnAreas[i].position, enemySpawnAreas[i].rotation, enemySpawnAreas[i]);
				enemy.name = "Enemy_" + (i + 1);
                
                Debug.Log("Spawned enemy " + (i + 1) + " at " + enemySpawnAreas[i].name);
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
		if (index >= 0 && index < creaturePrefabs.Length && index < creatureSpawnAreas.Length)
        {
			if (creaturePrefabs[index] != null && creatureSpawnAreas[index] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = creatureSpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(creatureSpawnAreas[index].GetChild(c).gameObject);
				}
                
				// Spawn new creature as child
				var creature = Instantiate(creaturePrefabs[index], creatureSpawnAreas[index].position, creatureSpawnAreas[index].rotation, creatureSpawnAreas[index]);
				creature.name = "Creature_" + (index + 1);
                
                Debug.Log("Respawned creature " + (index + 1));
            }
        }
    }
    
    // Method to respawn a specific enemy
	public void RespawnEnemy(int index)
    {
		if (index >= 0 && index < enemyPrefabs.Length && index < enemySpawnAreas.Length)
        {
			if (enemyPrefabs[index] != null && enemySpawnAreas[index] != null)
            {
				// Destroy existing children under this spawn area
				for (int c = enemySpawnAreas[index].childCount - 1; c >= 0; c--)
				{
					DestroyImmediate(enemySpawnAreas[index].GetChild(c).gameObject);
				}
                
				// Spawn new enemy as child
				var enemy = Instantiate(enemyPrefabs[index], enemySpawnAreas[index].position, enemySpawnAreas[index].rotation, enemySpawnAreas[index]);
				enemy.name = "Enemy_" + (index + 1);
                
                Debug.Log("Respawned enemy " + (index + 1));
            }
        }
    }
}
