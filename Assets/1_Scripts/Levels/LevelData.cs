using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum LevelType
{
    Enemy,
    Unknown,
    Shop,
    Treasure,
    Rest,
    Elite
}

[CreateAssetMenu(fileName = "New Level", menuName = "Levels/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Level ID - unique identifier for this level")]
    public string levelID = "";
    [TextArea(3, 5)]
    public string levelDescription = "";
    
    [Header("Level Type")]
    [Tooltip("The type of level this represents")]
    public LevelType levelType = LevelType.Unknown;
    
    [Header("Enemy Spawn Configuration")]
    [Tooltip("For each enemy spawn slot, assign multiple possible enemy unit data ScriptableObjects.")]
    public EnemySpawnSlot[] enemySpawnSlots = new EnemySpawnSlot[3];
    
    [Tooltip("If enabled, randomly selects from all assigned enemies for each slot. If disabled, uses the first assigned enemy for each slot.")]
    public bool useRandomEnemySpawns = false;
}
