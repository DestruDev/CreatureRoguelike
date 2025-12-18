using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the turn order timeline UI display
/// Shows current and upcoming turns based on action gauges
/// </summary>
public class TurnOrderTimeline : MonoBehaviour
{
    [Header("Turn Order UI")]
    [Tooltip("Turn order display slots (8 slots: index 0 = current turn at bottom, 7 = furthest future turn at top)")]
    public TextMeshProUGUI[] turnOrderSlots = new TextMeshProUGUI[8];
    
    private TurnOrder turnOrder;
    private GameManager gameManager;
    
    private void Start()
    {
        // Find references
        turnOrder = FindFirstObjectByType<TurnOrder>();
        gameManager = FindFirstObjectByType<GameManager>();
    }
    
    void Update()
    {
        // Update turn order UI display every frame
        UpdateTurnOrderUI();
    }
    
    /// <summary>
    /// Updates the turn order UI to show upcoming turns
    /// Slot 0 (bottom) = current turn
    /// Slots 1-7 (going up) = next turns in sequence
    /// </summary>
    private void UpdateTurnOrderUI()
    {
        if (turnOrderSlots == null || turnOrderSlots.Length != 8)
            return;
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
                return;
        }
        
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
            if (turnOrder == null)
                return;
        }
        
        // Initialize all slots first - keep them active and set to space to maintain height
        for (int i = 0; i < turnOrderSlots.Length; i++)
        {
            if (turnOrderSlots[i] != null)
            {
                turnOrderSlots[i].text = " "; // Space character to maintain line height
                turnOrderSlots[i].color = Color.white;
                turnOrderSlots[i].gameObject.SetActive(true);
            }
        }
        
        if (turnOrder.IsGameEnded())
        {
            return;
        }
        
        // Get current unit (slot 0 = bottom)
        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit != null && currentUnit.IsAlive())
        {
            if (turnOrderSlots[0] != null)
            {
                turnOrderSlots[0].text = currentUnit.UnitName;
                turnOrderSlots[0].color = Color.yellow; // Highlight current unit in yellow
                turnOrderSlots[0].gameObject.SetActive(true);
            }
        }
        
        // Get all alive units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            // No units found - clear all slots and return (prevents showing incorrect information during initialization)
            for (int i = 0; i < turnOrderSlots.Length; i++)
            {
                if (turnOrderSlots[i] != null)
                {
                    turnOrderSlots[i].text = " ";
                    turnOrderSlots[i].color = Color.white;
                }
            }
            return;
        }
        
        // Ensure we have at least one alive unit before displaying turn order
        bool hasAliveUnit = false;
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.IsAlive())
            {
                hasAliveUnit = true;
                break;
            }
        }
        
        if (!hasAliveUnit)
        {
            // No alive units - clear all slots and return
            for (int i = 0; i < turnOrderSlots.Length; i++)
            {
                if (turnOrderSlots[i] != null)
                {
                    turnOrderSlots[i].text = " ";
                    turnOrderSlots[i].color = Color.white;
                }
            }
            return;
        }
        
        // Simulate future turns to see which unit acts in each upcoming turn
        // This allows showing the same unit multiple times (e.g., fast units acting repeatedly)
        List<Unit> upcomingTurnOrder = new List<Unit>();
        
        // Create a snapshot of current gauge states for simulation
        Dictionary<Unit, float> simulatedGauges = new Dictionary<Unit, float>();
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.IsAlive())
            {
                // Use actual current gauge states
                simulatedGauges[unit] = unit.GetActionGauge();
            }
        }
        
        // Simulate up to 7 future turns
        // The current unit is acting now, so we start by simulating what happens AFTER they finish
        Unit simulatedLastActingUnit = currentUnit;
        
        // First, reset the current unit's gauge (they just finished acting)
        // This matches what happens in AdvanceToNextTurn
        if (currentUnit != null && simulatedGauges.ContainsKey(currentUnit))
        {
            float currentGauge = simulatedGauges[currentUnit];
            if (currentGauge > 100f)
            {
                // Preserve excess
                simulatedGauges[currentUnit] = currentGauge - 100f;
            }
            else
            {
                simulatedGauges[currentUnit] = 0f;
            }
        }
        
        for (int turn = 1; turn <= 7 && upcomingTurnOrder.Count < 7; turn++)
        {
            // Step 1: Increment all OTHER units (not the one who just acted)
            // This matches the real system: after a unit acts, all other units increment
            List<Unit> unitsToUpdate = new List<Unit>(simulatedGauges.Keys);
            foreach (var unit in unitsToUpdate)
            {
                if (unit != null && unit.IsAlive() && unit != simulatedLastActingUnit && simulatedGauges.ContainsKey(unit))
                {
                    simulatedGauges[unit] = simulatedGauges[unit] + unit.Speed;
                }
            }
            
            // Step 2: If no one can act yet, keep incrementing (just like real system)
            bool someoneCanAct = false;
            List<Unit> unitsToCheckForAction = new List<Unit>(simulatedGauges.Keys);
            foreach (var unit in unitsToCheckForAction)
            {
                if (simulatedGauges.ContainsKey(unit) && simulatedGauges[unit] >= 100f)
                {
                    someoneCanAct = true;
                    break;
                }
            }
            
            // Keep incrementing until someone can act
            int maxIncrementIterations = 20;
            int incrementIterations = 0;
            while (!someoneCanAct && incrementIterations < maxIncrementIterations)
            {
                incrementIterations++;
                
                // Increment all units except the one who just acted
                List<Unit> unitsToIncrement = new List<Unit>(simulatedGauges.Keys);
                foreach (var unit in unitsToIncrement)
                {
                    if (unit != null && unit.IsAlive() && unit != simulatedLastActingUnit && simulatedGauges.ContainsKey(unit))
                    {
                        simulatedGauges[unit] = simulatedGauges[unit] + unit.Speed;
                    }
                }
                
                // Check again
                unitsToCheckForAction = new List<Unit>(simulatedGauges.Keys);
                foreach (var unit in unitsToCheckForAction)
                {
                    if (simulatedGauges.ContainsKey(unit) && simulatedGauges[unit] >= 100f)
                    {
                        someoneCanAct = true;
                        break;
                    }
                }
            }
            
            // Step 3: Find the unit with highest gauge >= 100 (who will act next)
            Unit nextUnit = null;
            float highestGauge = -1f;
            
            List<Unit> unitsToCheck = new List<Unit>(simulatedGauges.Keys);
            foreach (var unit in unitsToCheck)
            {
                if (unit == null || !unit.IsAlive() || !simulatedGauges.ContainsKey(unit))
                    continue;
                    
                float gauge = simulatedGauges[unit];
                
                if (gauge >= 100f)
                {
                    // This unit can act
                    if (nextUnit == null || gauge > highestGauge)
                    {
                        nextUnit = unit;
                        highestGauge = gauge;
                    }
                    else if (Mathf.Approximately(gauge, highestGauge))
                    {
                        // Tie - use tiebreaker logic
                        if (turnOrder.CompareUnitsForTiebreaker(unit, nextUnit) < 0)
                        {
                            nextUnit = unit;
                            highestGauge = gauge;
                        }
                    }
                }
            }
            
            // If we found a unit that can act, add them to the list
            if (nextUnit != null)
            {
                upcomingTurnOrder.Add(nextUnit);
                
                // Reset that unit's gauge (preserving excess, just like in the real system)
                if (highestGauge > 100f)
                {
                    simulatedGauges[nextUnit] = highestGauge - 100f;
                }
                else
                {
                    simulatedGauges[nextUnit] = 0f;
                }
                
                // Update simulated last acting unit for next iteration
                simulatedLastActingUnit = nextUnit;
            }
            else
            {
                // No unit can act even after incrementing - this shouldn't happen if there are alive units
                // But if it does, break out of simulation
                break;
            }
        }
        
        // Fill slots 1-7 with the simulated turn order
        int slotIndex = 1;
        for (int i = 0; i < upcomingTurnOrder.Count && slotIndex < turnOrderSlots.Length; i++)
        {
            if (turnOrderSlots[slotIndex] != null)
            {
                Unit unit = upcomingTurnOrder[i];
                string displayName = GetDisplayNameForUnit(unit, allUnits);
                
                turnOrderSlots[slotIndex].text = displayName;
                turnOrderSlots[slotIndex].color = Color.white;
                turnOrderSlots[slotIndex].gameObject.SetActive(true);
                slotIndex++;
            }
        }
    }
    
    /// <summary>
    /// Gets a display name for a unit, adding a distinguishing identifier if there are duplicates
    /// </summary>
    private string GetDisplayNameForUnit(Unit unit, Unit[] allUnits)
    {
        string baseName = unit.UnitName;
        
        // Count how many units have the same name
        int sameNameCount = 0;
        foreach (var u in allUnits)
        {
            if (u != null && u.IsAlive() && u.UnitName == baseName)
            {
                sameNameCount++;
            }
        }
        
        // If there are duplicates, add a distinguishing identifier
        if (sameNameCount > 1)
        {
            // Sort all units with same name to get consistent numbering
            List<Unit> sameNameUnits = new List<Unit>();
            foreach (var u in allUnits)
            {
                if (u != null && u.IsAlive() && u.UnitName == baseName)
                {
                    sameNameUnits.Add(u);
                }
            }
            
            // Sort by team (player first), then by spawn index (if available), then by instance ID
            sameNameUnits.Sort((a, b) =>
            {
                // Use tiebreaker logic for consistent ordering
                int tiebreak = turnOrder != null ? turnOrder.CompareUnitsForTiebreaker(a, b) : 0;
                if (tiebreak != 0)
                    return tiebreak;
                // Final fallback: instance ID
                return a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            
            // Find this unit's index in the sorted list
            for (int i = 0; i < sameNameUnits.Count; i++)
            {
                if (sameNameUnits[i] == unit)
                {
                    return $"{baseName} ({i + 1})";
                }
            }
        }
        
        // No duplicates or couldn't find in list, return base name
        return baseName;
    }
}