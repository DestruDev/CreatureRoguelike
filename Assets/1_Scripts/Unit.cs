using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    [SerializeField] private CreatureUnitData creatureUnitData;
    
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    [SerializeField] private Image healthFill; // UI Image for health fill
    
    [Header("Runtime Stats")]
    private int currentHP;
    private int[] skillCooldowns;
    
    // Runtime team assignment override (doesn't modify ScriptableObject)
    private bool? teamAssignmentOverride = null;
    
    // Properties to access current data
    public bool HasData => creatureUnitData != null;
    
    // Current stats (cached from ScriptableObject)
    public int CurrentHP => currentHP;
    public int MaxHP => creatureUnitData != null ? creatureUnitData.maxHP : 0;
    public int AttackDamage => creatureUnitData != null ? creatureUnitData.attackDamage : 0;
    public int Defense => creatureUnitData != null ? creatureUnitData.defense : 0;
    public int Speed => creatureUnitData != null ? creatureUnitData.speed : 0;
    public Skill[] Skills => creatureUnitData != null ? creatureUnitData.skills : new Skill[0];
    public string UnitName => creatureUnitData != null && !string.IsNullOrEmpty(creatureUnitData.unitName) ? creatureUnitData.unitName : "Unknown";
    public string UnitID => creatureUnitData != null ? creatureUnitData.unitID : "";
    
    /// <summary>
    /// Gets the player unit status, checking runtime override first, then ScriptableObject data
    /// </summary>
    public bool IsPlayerUnit 
    { 
        get 
        {
            // If override is set, use it
            if (teamAssignmentOverride.HasValue)
            {
                return teamAssignmentOverride.Value;
            }
            // Otherwise, use ScriptableObject data (or default to false if no data)
            return creatureUnitData != null ? creatureUnitData.isPlayerUnit : false;
        }
    }
    
    public bool IsEnemyUnit => !IsPlayerUnit; // Helper property for targeting
    
    /// <summary>
    /// Override the team assignment for this unit instance at runtime (doesn't modify ScriptableObject)
    /// </summary>
    public void SetTeamAssignment(bool isPlayerUnit)
    {
        teamAssignmentOverride = isPlayerUnit;
    }
    
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
    
    /// <summary>
    /// Initialize the unit with ScriptableObject data (for runtime spawning)
    /// Team assignment is determined by spawn area, not by data type
    /// </summary>
    public void InitializeWithData(CreatureUnitData data)
    {
        this.creatureUnitData = data;
        
        // Get components if not already set
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Initialize unit data immediately (don't wait for Start)
        InitializeUnit();
    }
    
    /// <summary>
    /// Set the health fill UI image (for runtime setup)
    /// </summary>
    public void SetHealthFill(Image image)
    {
        healthFill = image;
    }
    
    bool ValidateUnitConfiguration()
    {
        // Check if unit data is assigned (both creatures and enemies use CreatureUnitData)
        if (creatureUnitData == null)
        {
            Debug.LogError("No CreatureUnitData is assigned to " + gameObject.name);
            return false;
        }
        
        return true;
    }
    
    void InitializeUnit()
    {
        // Set visual properties (both creatures and enemies use CreatureUnitData)
        if (spriteRenderer != null && creatureUnitData != null)
        {
            spriteRenderer.sprite = creatureUnitData.sprite;
            spriteRenderer.color = creatureUnitData.spriteColor;
        }
        
        // Initialize HP
        currentHP = MaxHP;
        UpdateHealthUI();
        
        // Initialize skill cooldowns
        skillCooldowns = new int[Skills.Length];
        
        Debug.Log(gameObject.name + " initialized with " + MaxHP + " HP and " + Skills.Length + " skills");
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
    
    public void UseSkill(int skillIndex, Unit target = null)
    {
        if (skillIndex < 0 || skillIndex >= Skills.Length)
        {
            Debug.Log("Invalid skill index");
            return;
        }
        
        if (skillCooldowns[skillIndex] > 0)
        {
            Debug.Log("Skill is on cooldown!");
            return;
        }
        
        Skill skill = Skills[skillIndex];
        
        // Set cooldown
        skillCooldowns[skillIndex] = skill.cooldownTurns;
        
        // Execute skill effects
        Debug.Log(gameObject.name + " uses " + skill.skillName + "!");
        
        // Apply skill effects based on effect type
        switch (skill.effectType)
        {
            case SkillEffectType.Damage:
                if (target != null && skill.damage > 0)
                {
                    target.TakeDamage(skill.damage);
                }
                break;
                
            case SkillEffectType.Heal:
                if (target != null && skill.healAmount > 0)
                {
                    target.Heal(skill.healAmount);
                }
                break;
                
            case SkillEffectType.Defend:
                Debug.Log(gameObject.name + " defends!");
                // Defense buff could be implemented here
                break;
                
            case SkillEffectType.Buff:
                Debug.Log(gameObject.name + " applies buff to " + (target != null ? target.gameObject.name : "self") + "!");
                // Buff could be implemented here
                break;
                
            case SkillEffectType.Debuff:
                Debug.Log(gameObject.name + " applies debuff to " + (target != null ? target.gameObject.name : "self") + "!");
                // Debuff could be implemented here
                break;
        }
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
        
        // Notify GameManager to update UI if health changed
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnUnitHealthChanged(this);
        }
    }
    
    public bool CanUseSkill(int skillIndex)
    {
        return skillIndex >= 0 && skillIndex < Skills.Length && skillCooldowns[skillIndex] == 0;
    }
    
    void Die()
    {
        Debug.Log(gameObject.name + " has died!");
        
        // Give rewards if this is an enemy unit
        if (IsEnemyUnit)
        {
            // Rewards system could be implemented here
            Debug.Log("Rewards system disabled for now");
        }
        
        // Hide the unit
        gameObject.SetActive(false);
    }
    
    // Turn-based methods
    public void StartTurn()
    {
        // Reduce cooldowns
        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            if (skillCooldowns[i] > 0)
            {
                skillCooldowns[i]--;
            }
        }
        
        Debug.Log(gameObject.name + " starts their turn!");
    }
}
