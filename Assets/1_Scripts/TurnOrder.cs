using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnOrder : MonoBehaviour
{
    /// <summary>
    /// Gets the unit that should go first based on speed.
    /// If multiple units have the same highest speed, one is picked randomly.
    /// </summary>
    /// <returns>The Unit with the highest speed (randomly chosen if tied)</returns>
    public Unit GetFirstUnit()
    {
        // Find all active units in the scene
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (allUnits == null || allUnits.Length == 0)
        {
            Debug.LogWarning("No units found in scene");
            return null;
        }

        // Find the highest speed value
        int maxSpeed = allUnits.Max(unit => unit.Speed);
        
        // Filter units with the highest speed
        List<Unit> fastestUnits = allUnits.Where(unit => unit.Speed == maxSpeed).ToList();
        
        // If there's only one, return it
        if (fastestUnits.Count == 1)
        {
            return fastestUnits[0];
        }
        
        // If there are ties, pick randomly
        int randomIndex = Random.Range(0, fastestUnits.Count);
        return fastestUnits[randomIndex];
    }

    /// <summary>
    /// Gets all units sorted by speed (highest first).
    /// Units with the same speed are randomly shuffled among themselves.
    /// </summary>
    /// <returns>List of units sorted by speed</returns>
    public List<Unit> GetTurnOrder()
    {
        // Find all active units in the scene
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (allUnits == null || allUnits.Length == 0)
        {
            Debug.LogWarning("No units found in scene");
            return new List<Unit>();
        }

        // Group units by speed
        var groupedBySpeed = allUnits.GroupBy(unit => unit.Speed)
                                     .OrderByDescending(group => group.Key);

        List<Unit> turnOrder = new List<Unit>();
        
        // For each speed group, shuffle and add to turn order
        foreach (var speedGroup in groupedBySpeed)
        {
            List<Unit> unitsAtThisSpeed = speedGroup.ToList();
            
            // Shuffle units with the same speed
            for (int i = unitsAtThisSpeed.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                Unit temp = unitsAtThisSpeed[i];
                unitsAtThisSpeed[i] = unitsAtThisSpeed[randomIndex];
                unitsAtThisSpeed[randomIndex] = temp;
            }
            
            turnOrder.AddRange(unitsAtThisSpeed);
        }
        
        return turnOrder;
    }
}
