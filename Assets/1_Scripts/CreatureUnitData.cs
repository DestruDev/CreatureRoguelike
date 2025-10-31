using UnityEngine;

[CreateAssetMenu(fileName = "New Creature Unit", menuName = "Unit/Creature")]
public class CreatureUnitData : ScriptableObject
{
    [Header("Visual")]
    public Sprite sprite;
    public Color spriteColor = Color.white;
    
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int attackDamage = 10;
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
