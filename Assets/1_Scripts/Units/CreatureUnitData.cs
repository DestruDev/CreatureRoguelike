using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Creature Unit", menuName = "Unit/Creature")]
public class CreatureUnitData : ScriptableObject
{
    [Header("Unit Info")]
    [Tooltip("Unit name - automatically set to asset name if left empty or set to default")]
    public string unitName = "Creature";
    public string unitID = "";
    // [Tooltip("If true, this unit belongs to the player team (can be controlled). If false, it's an enemy unit.")]
    // public bool isPlayerUnit = true;
    
    [Header("Visual")]
    public Sprite sprite;
    public Color spriteColor = Color.white;
    public Material spriteMaterial;
    public RuntimeAnimatorController animatorController;
    
    [Header("Stats")]
    public int maxHP = 100;
    [HideInInspector] public int currentHP;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10; // Turn order priority
    
    [Header("Skills")]
    public Skill[] skills = new Skill[0];
    
    // [Header("Rewards")]
    // public int experienceReward = 10;
    // public int goldReward = 5;
    
    // Initialize current HP to max HP when the asset is created
    private void OnEnable()
    {
        currentHP = maxHP;
    }
    
#if UNITY_EDITOR
    // Auto-set unitName to asset name if it's empty or still at default value
    private void OnValidate()
    {
        // Only auto-set if unitName is empty or still at default "Creature"
        if (string.IsNullOrEmpty(unitName) || unitName == "Creature")
        {
            // Get the asset path and extract just the filename without extension
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    unitName = fileName;
                }
            }
        }
    }
#endif
    
    // Method to reset HP to max (useful when spawning creatures)
    public void ResetHP()
    {
        currentHP = maxHP;
    }
    
    // Method to take damage
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHP = Mathf.Max(0, currentHP - actualDamage);
    }
    
    // Method to heal
    public void Heal(int healAmount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + healAmount);
    }
    
    // Check if creature is alive
    public bool IsAlive()
    {
        return currentHP > 0;
    }
    
    // Get HP percentage for UI bars
    public float GetHPPercentage()
    {
        return (float)currentHP / maxHP;
    }
}
