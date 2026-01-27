using UnityEngine;

namespace Map
{
    public enum NodeType
    {
        MinorEnemy,
        EliteEnemy,
        RestSite,
        Treasure,
        Store,
        Boss,
        Mystery
    }
}

namespace Map
{
    [CreateAssetMenu]
    public class NodeBlueprint : ScriptableObject
    {
        public Sprite sprite;
        public NodeType nodeType;
        
        [Header("Loot")]
        [Tooltip("LootTable that defines gold rewards and item drops for this node type")]
        public LootTable lootTable;
    }
}