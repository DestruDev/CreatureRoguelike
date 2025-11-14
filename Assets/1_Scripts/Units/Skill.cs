using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Skill", menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Skill name - automatically set to asset name if left empty or set to default")]
    public string skillName = "New Skill";
    public string skillID = "";
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
    
#if UNITY_EDITOR
    // Auto-set skillName to asset name if it's empty or still at default value
    private void OnValidate()
    {
        // Only auto-set if skillName is empty or still at default "New Skill"
        if (string.IsNullOrEmpty(skillName) || skillName == "New Skill")
        {
            // Get the asset path and extract just the filename without extension
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (!string.IsNullOrEmpty(fileName))
                {
                    skillName = fileName;
                }
            }
        }
    }
#endif
    
    // Method to execute the skill
    public void Execute(Unit caster, Unit target = null)
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
    
    // Check if skill can target the given unit
    public bool CanTarget(Unit target, Unit caster)
    {
        if (target == null) return false;
        
        // Check if target is enemy or ally based on team assignment
        bool isEnemy = caster != null && target.IsPlayerUnit != caster.IsPlayerUnit;
        bool isAlly = caster != null && target.IsPlayerUnit == caster.IsPlayerUnit;
        
        switch (targetType)
        {
            case SkillTargetType.Self:
                return target == caster;
            case SkillTargetType.Ally:
                return isAlly; // Includes self (same team)
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

