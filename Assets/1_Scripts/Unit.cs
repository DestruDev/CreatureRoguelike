using UnityEngine;
using UnityEngine.UI;

public enum UnitType
{
    Creature,
    Enemy
}

public class Unit : MonoBehaviour
{
    public UnitType unitType = UnitType.Creature;
    
    [SerializeField] private EnemyUnitData enemyData;
    [SerializeField] private CreatureUnitData creatureUnitData;
    
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Image healthFill; // UI Image for health fill
    
    [Header("Runtime Stats")]
    private int currentHP;
    private int[] abilityCooldowns;
    
    // Properties to access current data
    public bool IsEnemy => unitType == UnitType.Enemy;
    public bool IsCreature => unitType == UnitType.Creature;
    public bool HasData => (IsEnemy && enemyData != null) || (IsCreature && creatureUnitData != null);
    
    // Current stats (cached from ScriptableObject)
    public int CurrentHP => currentHP;
    public int MaxHP => IsEnemy ? enemyData.maxHP : creatureUnitData.maxHP;
    public int AttackDamage => IsEnemy ? enemyData.attackDamage : creatureUnitData.attackDamage;
    public int Defense => IsEnemy ? enemyData.defense : creatureUnitData.defense;
    public int Speed => IsEnemy ? enemyData.speed : creatureUnitData.speed;
    public Ability[] Abilities => IsEnemy ? enemyData.abilities : creatureUnitData.abilities;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Auto-find health fill image if not assigned
        if (healthFill == null)
        {
            var healthTransform = transform.Find("Canvas/HealthBar/Health");
            if (healthTransform != null)
            {
                healthFill = healthTransform.GetComponent<Image>();
            }
        }
        
        // Validate unit configuration
        if (!ValidateUnitConfiguration())
        {
            return;
        }
        
        // Initialize unit data
        InitializeUnit();
    }
    
    bool ValidateUnitConfiguration()
    {
        // Check if unit type matches assigned data
        if (IsEnemy && enemyData == null)
        {
            Debug.LogError("Unit type is set to Enemy but no EnemyUnitData is assigned to " + gameObject.name);
            return false;
        }
        
        if (IsCreature && creatureUnitData == null)
        {
            Debug.LogError("Unit type is set to Creature but no CreatureUnitData is assigned to " + gameObject.name);
            return false;
        }
        
        // Check for conflicting data assignments
        if (IsEnemy && creatureUnitData != null)
        {
            Debug.LogWarning("Unit type is Enemy but CreatureUnitData is assigned. Clearing CreatureUnitData.");
            creatureUnitData = null;
        }
        
        if (IsCreature && enemyData != null)
        {
            Debug.LogWarning("Unit type is Creature but EnemyUnitData is assigned. Clearing EnemyUnitData.");
            enemyData = null;
        }
        
        return true;
    }
    
    void InitializeUnit()
    {
        // Set visual properties
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = IsEnemy ? enemyData.sprite : creatureUnitData.sprite;
            spriteRenderer.color = IsEnemy ? enemyData.spriteColor : creatureUnitData.spriteColor;
        }
        
        // Initialize HP
        currentHP = MaxHP;
        UpdateHealthUI();
        
        // Initialize ability cooldowns
        abilityCooldowns = new int[Abilities.Length];
        
        Debug.Log(gameObject.name + " (" + unitType + ") initialized with " + MaxHP + " HP and " + Abilities.Length + " abilities");
    }
    
    // Combat methods
    public void TakeDamage(int damage)
    {
        int actualDamage = Mathf.Max(1, damage - Defense);
        currentHP = Mathf.Max(0, currentHP - actualDamage);
        UpdateHealthUI();
        
        Debug.Log(gameObject.name + " takes " + actualDamage + " damage! HP: " + currentHP + "/" + MaxHP);
        
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        currentHP = Mathf.Min(MaxHP, currentHP + healAmount);
        Debug.Log(gameObject.name + " heals for " + healAmount + " HP! HP: " + currentHP + "/" + MaxHP);
        UpdateHealthUI();
    }
    
    public void Attack(Unit target)
    {
        if (target == null || !target.IsAlive())
        {
            Debug.Log("Cannot attack - target is null or dead");
            return;
        }
        
        target.TakeDamage(AttackDamage);
        Debug.Log(gameObject.name + " attacks " + target.gameObject.name + " for " + AttackDamage + " damage!");
    }
    
    public void UseAbility(int abilityIndex, Unit target = null)
    {
        if (abilityIndex < 0 || abilityIndex >= Abilities.Length)
        {
            Debug.Log("Invalid ability index");
            return;
        }
        
        if (abilityCooldowns[abilityIndex] > 0)
        {
            Debug.Log("Ability is on cooldown!");
            return;
        }
        
        Ability ability = Abilities[abilityIndex];
        
        // Set cooldown
        abilityCooldowns[abilityIndex] = ability.cooldownTurns;
        
        // Execute ability (you'll need to adapt this based on your Ability.Execute method)
        Debug.Log(gameObject.name + " uses " + ability.abilityName + "!");
        
        // For now, just log the ability use
        // You can expand this to actually execute the ability effects
    }
    
    // Utility methods
    public bool IsAlive()
    {
        return currentHP > 0;
    }
    
    public float GetHPPercentage()
    {
        return (float)currentHP / MaxHP;
    }

    private void UpdateHealthUI()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount = Mathf.Clamp01(GetHPPercentage());
        }
    }
    
    public bool CanUseAbility(int abilityIndex)
    {
        return abilityIndex >= 0 && abilityIndex < Abilities.Length && abilityCooldowns[abilityIndex] == 0;
    }
    
    void Die()
    {
        Debug.Log(gameObject.name + " has died!");
        
        // Give rewards if this is an enemy
        if (IsEnemy)
        {
            // Debug.Log("Player gains " + enemyData.experienceReward + " XP and " + enemyData.goldReward + " gold!");
            Debug.Log("Rewards system disabled for now");
        }
        
        // Hide the unit
        gameObject.SetActive(false);
    }
    
    // Turn-based methods
    public void StartTurn()
    {
        // Reduce cooldowns
        for (int i = 0; i < abilityCooldowns.Length; i++)
        {
            if (abilityCooldowns[i] > 0)
            {
                abilityCooldowns[i]--;
            }
        }
        
        Debug.Log(gameObject.name + " starts their turn!");
    }
}
