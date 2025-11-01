using UnityEngine;
using System;

public class Creature : MonoBehaviour
{
    [Header("Unit Data")]
    public CreatureUnitData creatureUnitData;
    
    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    
    [Header("Turn-Based Stats")]
    private int[] skillCooldowns;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Apply unit data
        if (creatureUnitData != null)
        {
            ApplyCreatureData();
        }
        else
        {
            Debug.LogError("No CreatureUnitData assigned to " + gameObject.name + ".");
        }
    }
    
    void ApplyCreatureData()
    {
        if (creatureUnitData == null) return;
        
        // Set sprite and color
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = creatureUnitData.sprite;
            spriteRenderer.color = creatureUnitData.spriteColor;
        }
        
        // Reset HP
        creatureUnitData.ResetHP();
        
        // Initialize skill cooldowns
        skillCooldowns = new int[creatureUnitData.skills.Length];
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
    
    public void Attack(Creature target)
    {
        if (target == null || !target.IsAlive() || creatureUnitData == null)
        {
            Debug.Log("Cannot attack - target is null or dead, or no unit data assigned");
            return;
        }
        
        int dmg = creatureUnitData.attackDamage;
        target.TakeDamage(dmg);
        Debug.Log(gameObject.name + " attacks " + target.gameObject.name + " for " + dmg + " damage!");
    }
    
    public void UseSkill(int skillIndex, Creature target = null)
    {
        if (creatureUnitData == null)
        {
            Debug.Log("No unit data assigned");
            return;
        }
        
        var skills = creatureUnitData.skills;
        
        if (skillIndex < 0 || skillIndex >= skills.Length)
        {
            Debug.Log("Invalid skill index");
            return;
        }
        
        if (skillCooldowns[skillIndex] > 0)
        {
            Debug.Log("Skill is on cooldown!");
            return;
        }
        
        Skill skill = skills[skillIndex];
        
        // Check if target is valid
        if (!skill.CanTarget(target, this))
        {
            Debug.Log("Invalid target for skill: " + skill.skillName);
            return;
        }
        
        // Set cooldown
        skillCooldowns[skillIndex] = skill.cooldownTurns;
        
        // Execute skill
        skill.Execute(this, target);
    }
    
    public void TakeDamage(int damage)
    {
        if (creatureUnitData == null) return;
        
        creatureUnitData.TakeDamage(damage);
        Debug.Log(gameObject.name + " takes " + damage + " damage! HP: " + creatureUnitData.currentHP + "/" + creatureUnitData.maxHP);
        
        if (!IsAlive())
        {
            Die();
        }
    }
    
    public void Heal(int healAmount)
    {
        if (creatureUnitData == null) return;
        
        creatureUnitData.Heal(healAmount);
        Debug.Log(gameObject.name + " heals for " + healAmount + " HP! HP: " + creatureUnitData.currentHP + "/" + creatureUnitData.maxHP);
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
        // if (creatureUnitData != null)
        // {
        //     int xp = creatureUnitData.experienceReward;
        //     int gold = creatureUnitData.goldReward;
        //     Debug.Log("Player gains " + xp + " XP and " + gold + " gold!");
        // }
        Debug.Log("Rewards system disabled for now");
    }
    
    // Getters for UI
    public bool IsAlive()
    {
        return creatureUnitData != null && creatureUnitData.IsAlive();
    }
    
    public float GetHPPercentage()
    {
        return creatureUnitData != null ? creatureUnitData.GetHPPercentage() : 0f;
    }
    
    public bool CanUseSkill(int skillIndex)
    {
        if (creatureUnitData == null) return false;
        var skills = creatureUnitData.skills;
        return skillIndex >= 0 && skillIndex < skills.Length && skillCooldowns[skillIndex] == 0;
    }
}