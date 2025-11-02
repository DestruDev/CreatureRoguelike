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
        
        // Clear all slots first
        for (int i = 0; i < turnOrderSlots.Length; i++)
        {
            if (turnOrderSlots[i] != null)
            {
                turnOrderSlots[i].text = "";
                turnOrderSlots[i].gameObject.SetActive(false);
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
            return;
        
        // Create list of units with their "turns until action" calculation
        List<(Unit unit, float turnsUntilAction)> upcomingUnits = new List<(Unit, float)>();
        
        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;
            
            // Skip current unit (already displayed in slot 0)
            if (currentUnit != null && unit == currentUnit)
                continue;
            
            float gauge = unit.GetActionGauge();
            int speed = unit.Speed;
            
            // Calculate how many turns until this unit can act
            float turnsUntilAction;
            if (gauge >= 100f)
            {
                // Already at 100, will act next (or very soon if multiple units at 100)
                // Use gauge value to break ties (higher = acts first)
                turnsUntilAction = 0f - (gauge / 1000f); // Negative so it sorts first, with tiebreaker
            }
            else if (speed > 0)
            {
                // Calculate: (100 - gauge) / speed = turns needed
                // Round up to next integer turn
                turnsUntilAction = Mathf.Ceil((100f - gauge) / speed);
            }
            else
            {
                // Speed is 0, will never act
                turnsUntilAction = float.MaxValue;
            }
            
            upcomingUnits.Add((unit, turnsUntilAction));
        }
        
        // Sort by turns until action (lowest = acts soonest)
        // For tiebreakers (same turns until action), use tiebreaker logic
        upcomingUnits.Sort((a, b) =>
        {
            // First sort by turns until action
            int turnsCompare = a.turnsUntilAction.CompareTo(b.turnsUntilAction);
            if (turnsCompare != 0)
                return turnsCompare;
            
            // Same turns until action - use tiebreaker from TurnOrder
            return turnOrder.CompareUnitsForTiebreaker(a.unit, b.unit);
        });
        
        // Fill slots 1-7 with upcoming units (index 1 = next turn, index 7 = furthest)
        int slotIndex = 1;
        for (int i = 0; i < upcomingUnits.Count && slotIndex < turnOrderSlots.Length; i++)
        {
            if (turnOrderSlots[slotIndex] != null)
            {
                turnOrderSlots[slotIndex].text = upcomingUnits[i].unit.UnitName;
                turnOrderSlots[slotIndex].color = Color.white; // Reset to white for future turns
                turnOrderSlots[slotIndex].gameObject.SetActive(true);
                slotIndex++;
            }
        }
    }
}