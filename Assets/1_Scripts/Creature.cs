using UnityEngine;
using System;

public class Creature : MonoBehaviour
{
    [Header("Unit Data (assign one)")]
    public EnemyUnitData enemyData;
    public CreatureUnitData creatureUnitData;
    
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    
    [Header("Turn-Based Stats")]
    private int[] abilityCooldowns;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Apply unit data
        if (HasData())
        {
            ApplyCreatureData();
        }
        else
        {
            Debug.LogError("No UnitData assigned to " + gameObject.name + ". Assign either EnemyUnitData or CreatureUnitData.");
        }
    }
    
    void ApplyCreatureData()
    {
        // Set sprite and color
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = enemyData != null ? enemyData.sprite : creatureUnitData.sprite;
            spriteRenderer.color = enemyData != null ? enemyData.spriteColor : creatureUnitData.spriteColor;
        }
        
        // Reset HP
        if (enemyData != null) enemyData.ResetHP(); else creatureUnitData.ResetHP();
        
        // Initialize ability cooldowns
        var abilities = enemyData != null ? enemyData.abilities : creatureUnitData.abilities;
        abilityCooldowns = new int[abilities.Length];
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
    
    public void Attack(Creature target)
    {
        if (target == null || !target.IsAlive())
        {
            Debug.Log("Cannot attack - target is null or dead");
            return;
        }
        
        int dmg = enemyData != null ? enemyData.attackDamage : creatureUnitData.attackDamage;
        target.TakeDamage(dmg);
        Debug.Log(gameObject.name + " attacks " + target.gameObject.name + " for " + dmg + " damage!");
    }
    
    public void UseAbility(int abilityIndex, Creature target = null)
    {
        var abilities = enemyData != null ? enemyData.abilities : creatureUnitData.abilities;
        
        if (abilityIndex < 0 || abilityIndex >= abilities.Length)
        {
            Debug.Log("Invalid ability index");
            return;
        }
        
        if (abilityCooldowns[abilityIndex] > 0)
        {
            Debug.Log("Ability is on cooldown!");
            return;
        }
        
        Ability ability = abilities[abilityIndex];
        
        // Check if target is valid
        if (!ability.CanTarget(target, this))
        {
            Debug.Log("Invalid target for ability: " + ability.abilityName);
            return;
        }
        
        // Set cooldown
        abilityCooldowns[abilityIndex] = ability.cooldownTurns;
        
        // Execute ability
        ability.Execute(this, target);
    }
    
    public void TakeDamage(int damage)
    {
        if (enemyData != null) enemyData.TakeDamage(damage); else creatureUnitData.TakeDamage(damage);
        int hp = enemyData != null ? enemyData.currentHP : creatureUnitData.currentHP;
        int maxHp = enemyData != null ? enemyData.maxHP : creatureUnitData.maxHP;
        Debug.Log(gameObject.name + " takes " + damage + " damage! HP: " + hp + "/" + maxHp);
        
        if (!IsAlive())
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        if (enemyData != null) enemyData.Heal(healAmount); else creatureUnitData.Heal(healAmount);
        int hp = enemyData != null ? enemyData.currentHP : creatureUnitData.currentHP;
        int maxHp = enemyData != null ? enemyData.maxHP : creatureUnitData.maxHP;
        Debug.Log(gameObject.name + " heals for " + healAmount + " HP! HP: " + hp + "/" + maxHp);
    }
    
    void Die()
    {
        Debug.Log(gameObject.name + " has died!");
        
        // Give rewards to player
        GiveRewards();
        
        // Hide the creature (don't destroy for turn-based)
        gameObject.SetActive(false);
    }
    
    void GiveRewards()
    {
        // This would typically interact with a player or game manager
        // int xp = enemyData != null ? enemyData.experienceReward : creatureUnitData.experienceReward;
        // int gold = enemyData != null ? enemyData.goldReward : creatureUnitData.goldReward;
        // Debug.Log("Player gains " + xp + " XP and " + gold + " gold!");
        Debug.Log("Rewards system disabled for now");
    }
    
    // Getters for UI
    public bool IsAlive()
    {
        return enemyData != null ? enemyData.IsAlive() : (creatureUnitData != null && creatureUnitData.IsAlive());
    }
    
    public float GetHPPercentage()
    {
        return enemyData != null ? enemyData.GetHPPercentage() : (creatureUnitData != null ? creatureUnitData.GetHPPercentage() : 0f);
    }
    
    public bool CanUseAbility(int abilityIndex)
    {
        var abilities = enemyData != null ? enemyData.abilities : (creatureUnitData != null ? creatureUnitData.abilities : new Ability[0]);
        return abilityIndex >= 0 && abilityIndex < abilities.Length && abilityCooldowns[abilityIndex] == 0;
    }

    private bool HasData()
    {
        return enemyData != null || creatureUnitData != null;
    }
}