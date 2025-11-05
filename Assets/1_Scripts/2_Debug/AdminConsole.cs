using UnityEngine;

public class AdminConsole : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Amount of damage to deal when using admin functions")]
    public int damageAmount = 20;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Press 1 to damage all enemies
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DamageAllEnemies();
        }
        
        // Press 2 to damage all allies
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            DamageAllAllies();
        }
    }
    
    /// <summary>
    /// Damages all enemy units in the scene
    /// </summary>
    public void DamageAllEnemies()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int enemyCount = 0;
        
        foreach (var unit in allUnits)
        {
            if (unit.IsEnemyUnit && unit.IsAlive())
            {
                unit.TakeDamageIgnoreDefense(damageAmount);
                enemyCount++;
            }
        }
        
        Debug.Log($"Admin: Damaged {enemyCount} enemies for {damageAmount} damage each");
    }
    
    /// <summary>
    /// Damages all ally (player) units in the scene
    /// </summary>
    public void DamageAllAllies()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int allyCount = 0;
        
        foreach (var unit in allUnits)
        {
            if (unit.IsPlayerUnit && unit.IsAlive())
            {
                unit.TakeDamageIgnoreDefense(damageAmount);
                allyCount++;
            }
        }
        
        Debug.Log($"Admin: Damaged {allyCount} allies for {damageAmount} damage each");
    }
}
