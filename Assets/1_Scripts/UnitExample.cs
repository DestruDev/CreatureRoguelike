using UnityEngine;

/// <summary>
/// Example script showing how to use the Unit component with ScriptableObjects
/// </summary>
public class UnitExample : MonoBehaviour
{
    [Header("Example Usage")]
    public Unit exampleUnit;
    
    void Start()
    {
        if (exampleUnit != null)
        {
            DemonstrateUnitUsage();
        }
    }
    
    void DemonstrateUnitUsage()
    {
        Debug.Log("=== Unit Example ===");
        Debug.Log("Unit Name: " + exampleUnit.gameObject.name);
        Debug.Log("Unit Type: " + exampleUnit.unitType);
        Debug.Log("Is Enemy: " + exampleUnit.IsEnemy);
        Debug.Log("Is Creature: " + exampleUnit.IsCreature);
        Debug.Log("Max HP: " + exampleUnit.MaxHP);
        Debug.Log("Attack Damage: " + exampleUnit.AttackDamage);
        Debug.Log("Number of Skills: " + exampleUnit.Skills.Length);
        
        // Example of using skills
        for (int i = 0; i < exampleUnit.Skills.Length; i++)
        {
            if (exampleUnit.CanUseSkill(i))
            {
                Debug.Log("Can use skill: " + exampleUnit.Skills[i].skillName);
            }
        }
    }
    
    // Example method to simulate combat
    public void SimulateCombat(Unit target)
    {
        if (exampleUnit != null && target != null)
        {
            Debug.Log("=== Combat Simulation ===");
            Debug.Log(exampleUnit.gameObject.name + " attacks " + target.gameObject.name);
            exampleUnit.Attack(target);
            
            if (target.IsAlive())
            {
                Debug.Log(target.gameObject.name + " HP: " + target.CurrentHP + "/" + target.MaxHP);
            }
            else
            {
                Debug.Log(target.gameObject.name + " has been defeated!");
            }
        }
    }
}
