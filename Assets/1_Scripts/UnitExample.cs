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
        Debug.Log("Number of Abilities: " + exampleUnit.Abilities.Length);
        
        // Example of using abilities
        for (int i = 0; i < exampleUnit.Abilities.Length; i++)
        {
            if (exampleUnit.CanUseAbility(i))
            {
                Debug.Log("Can use ability: " + exampleUnit.Abilities[i].abilityName);
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
