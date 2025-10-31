using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName = "New Skill";
    [TextArea(3, 5)]
    public string description = "Skill description";
    public Sprite icon;
    
    [Header("Stats")]
    public int damage = 0;
    public int healAmount = 0;
    public int cooldownTurns = 0;
    
    [Header("Targeting")]
    public SkillTargetType targetType = SkillTargetType.Self;
    
    [Header("Effects")]
    public SkillEffectType effectType = SkillEffectType.Damage;
	// Duration removed; effects should be handled externally if needed
    
    // [Header("Animation")]
    // public string animationTrigger = "";
    // public float animationDuration = 1f;
    
    // Method to execute the skill
    public void Execute(Creature caster, Creature target = null)
    {
        switch (effectType)
        {
            case SkillEffectType.Damage:
                if (target != null && damage > 0)
                {
                    target.TakeDamage(damage);
                    Debug.Log(caster.gameObject.name + " deals " + damage + " damage to " + target.gameObject.name + "!");
                }
                break;
                
            case SkillEffectType.Heal:
                if (target != null && healAmount > 0)
                {
                    target.Heal(healAmount);
                    Debug.Log(caster.gameObject.name + " heals " + target.gameObject.name + " for " + healAmount + " HP!");
                }
                break;
                
            case SkillEffectType.Defend:
				// Increase defense; amount/duration handled elsewhere
				Debug.Log(caster.gameObject.name + " defends!");
                break;
                
            case SkillEffectType.Buff:
                // Apply buff for duration turns
                Debug.Log(caster.gameObject.name + " applies buff to " + (target != null ? target.gameObject.name : "self") + "!");
                break;
                
            case SkillEffectType.Debuff:
                // Apply debuff for duration turns
                Debug.Log(caster.gameObject.name + " applies debuff to " + (target != null ? target.gameObject.name : "self") + "!");
                break;
        }
    }
    
    // Check if skill can target the given creature
    public bool CanTarget(Creature target, Creature caster)
    {
        if (target == null) return false;
        
        // Check if target is enemy or ally
        bool isEnemy = true; // You'll need to implement team checking
        bool isAlly = !isEnemy;
        
        switch (targetType)
        {
            case SkillTargetType.Self:
                return target == caster;
            case SkillTargetType.Ally:
                return isAlly;
            case SkillTargetType.Enemy:
                return isEnemy;
            case SkillTargetType.Any:
                return true;
            default:
                return false;
        }
    }
}

public enum SkillTargetType
{
    Self,       // Can only target self
    Ally,       // Can only target allies
    Enemy,      // Can only target enemies
    Any         // Can target anyone
}

public enum SkillEffectType
{
    Damage,     // Deal damage
    Heal,       // Restore HP
    Defend,     // Increase defense
    Buff,       // Apply positive effect
    Debuff      // Apply negative effect
}

