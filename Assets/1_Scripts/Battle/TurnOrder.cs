using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TurnOrder : MonoBehaviour
{
    [Header("Unit SpriteRenderers")]
    public SpriteRenderer unit1Renderer;
    public SpriteRenderer unit2Renderer;
    public SpriteRenderer unit3Renderer;
    public SpriteRenderer unit4Renderer;
    public SpriteRenderer unit5Renderer;
    public SpriteRenderer unit6Renderer;

    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    [Range(0f, 100f)]
    public float transparencyPercentage = 50f;
    

    private SpriteRenderer[] unitRenderers;
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private GameManager gameManager;
    private List<Unit> cachedTurnOrder = null; // Cached turn order to ensure deterministic sequence
    private bool isUnitActing = false; // Prevents multiple units from acting simultaneously
    private bool isSelectingNextUnit = false; // Prevents infinite recursion in SelectNextUnitToAct
    private bool gameEnded = false; // Set to true when all player units or all enemy units are dead

    private void Start()
    {
        // Initialize arrays
        unitRenderers = new SpriteRenderer[] 
        { 
            unit1Renderer, unit2Renderer, unit3Renderer, 
            unit4Renderer, unit5Renderer, unit6Renderer 
        };

        // Store original colors
        foreach (var renderer in unitRenderers)
        {
            if (renderer != null)
            {
                originalColors[renderer] = renderer.color;
            }
        }

        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        UpdateHighlight();
        
        // Only continue selecting units if game hasn't ended
        if (!gameEnded)
        {
            SelectNextUnitToAct();
        }
    }

    /// <summary>
    /// Updates the highlight color/transparency on the current unit's spawn location capsule renderer (unit1-6)
    /// </summary>
    private void UpdateHighlight()
    {
        // If game has ended, reset all highlights to normal
        if (gameEnded)
        {
            foreach (var renderer in unitRenderers)
            {
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    renderer.color = originalColors[renderer];
                }
            }
            return;
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();

        // Reset all renderers to original colors first
        foreach (var renderer in unitRenderers)
        {
            if (renderer != null)
            {
                // Ensure we have the original color stored
                if (!originalColors.ContainsKey(renderer))
                {
                    originalColors[renderer] = renderer.color;
                }
                
                // Check if this is NOT the current unit, then reset to original
                bool isCurrentUnit = currentUnit != null && FindUnitRenderer(currentUnit) == renderer;
                if (!isCurrentUnit)
                {
                    renderer.color = originalColors[renderer];
                }
            }
        }

        // If there's a current unit, highlight its spawn location capsule renderer
        if (currentUnit != null)
        {
            SpriteRenderer currentRenderer = FindUnitRenderer(currentUnit);
            if (currentRenderer != null)
            {
                // Ensure we have the original color stored
                if (!originalColors.ContainsKey(currentRenderer))
                {
                    originalColors[currentRenderer] = currentRenderer.color;
                }
                
                // Get original color
                Color originalColor = originalColors[currentRenderer];
                
                // Apply highlight color with transparency percentage
                float alpha = transparencyPercentage / 100f;
                
                // Use highlight color RGB directly with the transparency percentage as alpha
                Color finalColor = new Color(
                    highlightColor.r, 
                    highlightColor.g, 
                    highlightColor.b, 
                    alpha
                );
                
                currentRenderer.color = finalColor;
            }
        }
    }

    /// <summary>
    /// Finds the SpriteRenderer (spawn location capsule) associated with a given Unit
    /// Units are spawned as children of the spawn area GameObjects, so we check the unit's parent hierarchy
    /// </summary>
    private SpriteRenderer FindUnitRenderer(Unit unit)
    {
        if (unit == null) return null;

        // Check each of our assigned renderers (unit1-6 spawn location capsules)
        foreach (var renderer in unitRenderers)
        {
            if (renderer != null)
            {
                // Check if the unit is a child (directly or indirectly) of the renderer's GameObject
                // This works because units are spawned as children of the spawn areas
                Transform unitTransform = unit.transform;
                while (unitTransform != null)
                {
                    if (unitTransform.gameObject == renderer.gameObject)
                    {
                        // The unit is a descendant of this spawn area
                        return renderer;
                    }
                    unitTransform = unitTransform.parent;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the deterministic spawn position index (0-5) for a unit based on which renderer it belongs to
    /// Returns -1 if the unit doesn't belong to any known spawn area
    /// </summary>
    private int GetUnitSpawnIndex(Unit unit)
    {
        SpriteRenderer renderer = FindUnitRenderer(unit);
        if (renderer == null) return -1;

        // Find which index (0-5) this renderer is in our array
        for (int i = 0; i < unitRenderers.Length; i++)
        {
            if (unitRenderers[i] == renderer)
            {
                return i;
            }
        }

        return -1;
    }
    
    /// <summary>
    /// Checks if the game should end (all player units or all enemy units are dead)
    /// Returns true if game should continue, false if game has ended
    /// </summary>
    private bool CheckGameEnd()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            gameEnded = true;
            Debug.Log("Game Over: No units remaining!");
            return false;
        }
        
        bool hasAlivePlayerUnits = false;
        bool hasAliveEnemyUnits = false;
        
        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;
                
            if (unit.IsPlayerUnit)
                hasAlivePlayerUnits = true;
            else
                hasAliveEnemyUnits = true;
        }
        
        if (!hasAlivePlayerUnits)
        {
            gameEnded = true;
            Debug.Log("Game Over: All player units are dead!");
            
            // Notify GameManager to open the all allies dead panel
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
            if (gameManager != null)
            {
                gameManager.OnAllAlliesDead();
            }
            
            return false;
        }
        
        if (!hasAliveEnemyUnits)
        {
            gameEnded = true;
            Debug.Log("Round Won: All enemy units are dead!");
            
            // Notify GameManager to open the round end panel with "Round Won!" message
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }
            if (gameManager != null)
            {
                gameManager.OnAllEnemiesDead();
            }
            
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Selects the unit that should act next based on their action gauge values
    /// This ONLY checks which unit has the highest gauge >= 100 - it does NOT increment gauges
    /// </summary>
    private void SelectNextUnitToAct()
    {
        // Prevent infinite recursion
        if (isSelectingNextUnit)
        {
            return;
        }
        
        // Stop if game has ended
        if (gameEnded)
        {
            return;
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
                return;
        }
        
        // Check if game should end before selecting next unit
        if (!CheckGameEnd())
        {
            isSelectingNextUnit = false;
            return;
        }
        
        isSelectingNextUnit = true;

        // Find all alive units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            isSelectingNextUnit = false;
            return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();
        
        // If current unit is dead, clear the acting flag and allow selection of a new unit
        if (currentUnit != null && !currentUnit.IsAlive())
        {
            Debug.LogWarning($"Current unit {currentUnit.gameObject.name} is dead! Clearing acting flag.");
            isUnitActing = false;
            // Continue to select a new unit
        }
        // Only select a new unit if no unit is currently acting (and current unit is alive)
        else if (isUnitActing && currentUnit != null && currentUnit.IsAlive())
        {
            isSelectingNextUnit = false;
            return;
        }
        
        // Find the unit with the highest gauge that has reached 100
        // Tiebreaker: Player units > Enemy units, then by spawn index (1-3 for players, 4-6 for enemies)
        Unit nextUnitToAct = null;
        float highestGauge = -1f;

        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;

            // Check if this unit can act (gauge >= 100)
            if (unit.GetActionGauge() >= 100f)
            {
                // Don't select the current unit if it's still acting
                if (currentUnit != null && unit == currentUnit)
                    continue;
                
                float unitGauge = unit.GetActionGauge();
                
                // Select this unit if:
                // 1. No unit selected yet, OR
                // 2. This unit has higher gauge, OR
                // 3. Same gauge and this unit wins tiebreaker (player > enemy, then lower spawn index)
                bool shouldSelect = false;
                
                if (nextUnitToAct == null)
                {
                    shouldSelect = true;
                }
                else if (unitGauge > highestGauge)
                {
                    shouldSelect = true;
                }
                else if (Mathf.Approximately(unitGauge, highestGauge))
                {
                    // Same gauge - use tiebreaker
                    shouldSelect = CompareUnitsForTiebreaker(unit, nextUnitToAct) < 0;
                }
                
                if (shouldSelect)
                {
                    nextUnitToAct = unit;
                    highestGauge = unitGauge;
                }
            }
        }

        // If we found a unit ready to act, start their turn
        if (nextUnitToAct != null)
        {
            // Final safety check - make sure the unit is still alive
            if (!nextUnitToAct.IsAlive())
            {
                Debug.LogWarning($"Selected unit {nextUnitToAct.gameObject.name} is dead! Trying to find another unit...");
                // Try again, excluding dead units
                isSelectingNextUnit = false;
                SelectNextUnitToAct();
                return;
            }
            
            //Debug.Log($"Selecting next unit: {nextUnitToAct.gameObject.name} with gauge {highestGauge}");
            isUnitActing = true;
            gameManager.SetCurrentUnit(nextUnitToAct);
        }
        // Note: We do NOT increment gauges here - that only happens in AdvanceToNextTurn()
        // If no unit can act, we just wait (AdvanceToNextTurn will handle incrementing)
        
        isSelectingNextUnit = false; // Reset flag at the end
    }

    /// <summary>
    /// Public method for comparing units in tiebreaker situations
    /// Used by TurnOrderTimeline for sorting upcoming turns
    /// Returns: <0 if unit1 should go first, >0 if unit2 should go first, 0 if equal
    /// Priority: Player units > Enemy units, then lower spawn index
    /// </summary>
    public int CompareUnitsForTiebreaker(Unit unit1, Unit unit2)
    {
        // First priority: Player units go before enemy units
        bool unit1IsPlayer = unit1.IsPlayerUnit;
        bool unit2IsPlayer = unit2.IsPlayerUnit;
        
        if (unit1IsPlayer && !unit2IsPlayer)
            return -1; // unit1 (player) goes first
        if (!unit1IsPlayer && unit2IsPlayer)
            return 1;  // unit2 (player) goes first
        
        // Same team - compare by spawn index (lower index = earlier slot)
        int index1 = GetUnitSpawnIndex(unit1);
        int index2 = GetUnitSpawnIndex(unit2);
        
        // If both have valid spawn indices, sort by index (lower = earlier)
        if (index1 >= 0 && index2 >= 0)
        {
            return index1.CompareTo(index2);
        }
        
        // If only one has a valid index, it comes first
        if (index1 >= 0) return -1;
        if (index2 >= 0) return 1;
        
        // If neither has a valid index, use instance ID for deterministic ordering
        return unit1.GetInstanceID().CompareTo(unit2.GetInstanceID());
    }
    
    /// <summary>
    /// Gets the unit that should go first based on action gauge
    /// Uses tiebreaker: Player units > Enemy units, then by spawn index (1-3 for players, 4-6 for enemies)
    /// </summary>
    /// <returns>The Unit with the highest action gauge that can act</returns>
    public Unit GetFirstUnit()
    {
        // Find all alive units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (allUnits == null || allUnits.Length == 0)
        {
            Debug.LogWarning("No units found");
            return null;
        }

        // Find the unit with the highest gauge that has reached 100
        Unit firstUnit = null;
        float highestGauge = 0f;

        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;

            float unitGauge = unit.GetActionGauge();
            
            if (unitGauge >= 100f)
            {
                // Select this unit if:
                // 1. No unit selected yet, OR
                // 2. This unit has higher gauge, OR
                // 3. Same gauge and this unit wins tiebreaker
                bool shouldSelect = false;
                
                if (firstUnit == null)
                {
                    shouldSelect = true;
                }
                else if (unitGauge > highestGauge)
                {
                    shouldSelect = true;
                }
                else if (Mathf.Approximately(unitGauge, highestGauge))
                {
                    // Same gauge - use tiebreaker
                    shouldSelect = CompareUnitsForTiebreaker(unit, firstUnit) < 0;
                }
                
                if (shouldSelect)
                {
                    firstUnit = unit;
                    highestGauge = unitGauge;
                }
            }
        }

        // If no unit has reached 100 yet, return the one with the highest gauge
        if (firstUnit == null)
        {
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive())
                    continue;

                float unitGauge = unit.GetActionGauge();
                
                bool shouldSelect = false;
                
                if (firstUnit == null)
                {
                    shouldSelect = true;
                }
                else if (unitGauge > highestGauge)
                {
                    shouldSelect = true;
                }
                else if (Mathf.Approximately(unitGauge, highestGauge))
                {
                    // Same gauge - use tiebreaker
                    shouldSelect = CompareUnitsForTiebreaker(unit, firstUnit) < 0;
                }
                
                if (shouldSelect)
                {
                    firstUnit = unit;
                    highestGauge = unitGauge;
                }
            }
        }
        
        if (firstUnit != null)
        {
            string tiebreakerInfo = "";
            Unit[] tiebreakerUnits = allUnits.Where(u => u != null && u.IsAlive() && Mathf.Approximately(u.GetActionGauge(), highestGauge)).ToArray();
            if (tiebreakerUnits.Length > 1)
            {
                tiebreakerInfo = $" (tiebreaker: {(firstUnit.IsPlayerUnit ? "Player" : "Enemy")} slot {GetUnitSpawnIndex(firstUnit) + 1})";
            }
            //Debug.Log(firstUnit.gameObject.name + " goes first! (Speed: " + firstUnit.Speed + ", Gauge: " + firstUnit.GetActionGauge() + tiebreakerInfo + ")");
        }
        
        return firstUnit;
    }

    /// <summary>
    /// Gets all units sorted by speed (highest first).
    /// Units with the same speed are ordered deterministically by their spawn position (unit1-6 order).
    /// The turn order is cached to ensure it remains consistent across turns.
    /// </summary>
    /// <param name="forceRecalculate">If true, forces recalculation of the turn order</param>
    /// <returns>List of units sorted by speed, then by spawn position</returns>
    public List<Unit> GetTurnOrder(bool forceRecalculate = false)
    {
        // Return cached turn order if it exists and we're not forcing recalculation
        if (!forceRecalculate && cachedTurnOrder != null && cachedTurnOrder.Count > 0)
        {
            // Verify all cached units still exist and are valid
            bool allValid = true;
            foreach (var unit in cachedTurnOrder)
            {
                if (unit == null)
                {
                    allValid = false;
                    break;
                }
            }
            
            if (allValid)
            {
                return new List<Unit>(cachedTurnOrder); // Return a copy to prevent external modification
            }
        }

        // Find all active units in the scene
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (allUnits == null || allUnits.Length == 0)
        {
            Debug.LogWarning("No units found in scene");
            cachedTurnOrder = new List<Unit>();
            return new List<Unit>();
        }

        // Group units by speed
        var groupedBySpeed = allUnits.GroupBy(unit => unit.Speed)
                                     .OrderByDescending(group => group.Key);

        List<Unit> turnOrder = new List<Unit>();
        
        // For each speed group, order deterministically by spawn position and add to turn order
        foreach (var speedGroup in groupedBySpeed)
        {
            List<Unit> unitsAtThisSpeed = speedGroup.ToList();
            
            // Sort units with the same speed by their spawn index (deterministic order)
            // Units without a known spawn index go last, sorted by instance ID as tiebreaker
            unitsAtThisSpeed.Sort((unit1, unit2) =>
            {
                int index1 = GetUnitSpawnIndex(unit1);
                int index2 = GetUnitSpawnIndex(unit2);
                
                // If both have valid spawn indices, sort by index
                if (index1 >= 0 && index2 >= 0)
                {
                    return index1.CompareTo(index2);
                }
                
                // If only one has a valid index, it comes first
                if (index1 >= 0) return -1;
                if (index2 >= 0) return 1;
                
                // If neither has a valid index, use instance ID for deterministic ordering
                return unit1.GetInstanceID().CompareTo(unit2.GetInstanceID());
            });
            
            turnOrder.AddRange(unitsAtThisSpeed);
        }
        
        // Cache the turn order
        cachedTurnOrder = new List<Unit>(turnOrder);
        
        return turnOrder;
    }

    /// <summary>
    /// Invalidates the cached turn order, forcing it to be recalculated on next call
    /// Call this when units are spawned, killed, or when speeds change
    /// </summary>
    public void InvalidateTurnOrder()
    {
        cachedTurnOrder = null;
    }
    
    /// <summary>
    /// Sets the acting flag to indicate a unit is currently acting
    /// Called when manually setting a unit (like at game start)
    /// </summary>
    public void SetUnitActing(bool acting)
    {
        isUnitActing = acting;
    }

    /// <summary>
    /// Advances to the next unit's turn by:
    /// 1. Resetting the current unit's action gauge
    /// 2. Incrementing all units' action gauges based on their speed
    /// 3. Selecting the next unit with the highest gauge >= 100
    /// </summary>
    public void AdvanceToNextTurn()
    {
        // Stop if game has ended
        if (gameEnded)
        {
            return;
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("Cannot advance turn - GameManager not found!");
                return;
            }
        }

        // Check if game should end before advancing
        if (!CheckGameEnd())
        {
            isUnitActing = false;
            return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("Cannot advance turn - no current unit!");
            isUnitActing = false;
            return;
        }

        // Check if current unit is dead - if so, just advance without resetting gauge
        if (!currentUnit.IsAlive())
        {
            Debug.LogWarning($"Current unit {currentUnit.gameObject.name} is dead! Skipping gauge reset and advancing.");
            isUnitActing = false;
            
            // Check if game should end
            if (!CheckGameEnd())
            {
                return;
            }
            
            // Find all alive units to increment
            Unit[] aliveUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (aliveUnits != null)
            {
                // Increment all alive units
                foreach (var unit in aliveUnits)
                {
                    if (unit != null && unit.IsAlive())
                    {
                        unit.IncrementActionGauge();
                    }
                }
            }
            
            SelectNextUnitToAct();
            return;
        }

        // Get gauge before reset to log excess
        float gaugeBeforeReset = currentUnit.GetActionGauge();
        
        // Reset the current unit's action gauge after they've acted
        currentUnit.ResetActionGauge();
        
        float gaugeAfterReset = currentUnit.GetActionGauge();
        
        // Increment all units' action gauges based on their speed (once per turn)
        // If no unit can act after incrementing, continue incrementing until someone can
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        Debug.Log($"=== Advancing turn from {currentUnit.gameObject.name} ===");
        
        // Allow the system to select the next unit to act
        isUnitActing = false;
        
        if (allUnits != null)
        {
            // First, increment all other units once
            foreach (var unit in allUnits)
            {
                if (unit != null && unit.IsAlive() && unit != currentUnit)
                {
                    float oldGauge = unit.GetActionGauge();
                    unit.IncrementActionGauge();
                    float newGauge = unit.GetActionGauge();
                    //Debug.Log($"{unit.gameObject.name} (Speed {unit.Speed}): Gauge {oldGauge} -> {newGauge}");
                }
            }
            
            // Check if anyone can act now
            bool someoneCanAct = false;
            foreach (var unit in allUnits)
            {
                if (unit != null && unit.IsAlive() && unit.GetActionGauge() >= 100f)
                {
                    someoneCanAct = true;
                    break;
                }
            }
            
            // If no one can act yet, continue incrementing until someone reaches 100
            if (!someoneCanAct)
            {
                Debug.Log("No unit can act after first increment. Continuing to increment...");
                int maxIterations = 10;
                int iterations = 0;
                
                while (!someoneCanAct && iterations < maxIterations && !gameEnded)
                {
                    iterations++;
                    
                    // Check game end before each iteration
                    if (!CheckGameEnd())
                    {
                        break;
                    }
                    
                    foreach (var unit in allUnits)
                    {
                        if (unit == null || !unit.IsAlive() || unit == currentUnit)
                            continue;
                        
                        float oldGauge = unit.GetActionGauge();
                        bool reached100 = unit.IncrementActionGauge();
                        float newGauge = unit.GetActionGauge();
                        
                        Debug.Log($"{unit.gameObject.name} (Speed {unit.Speed}): Gauge {oldGauge} -> {newGauge}");
                        
                        if (reached100)
                        {
                            someoneCanAct = true;
                        }
                    }
                }
                
                if (iterations >= maxIterations && !someoneCanAct && !gameEnded)
                {
                    Debug.LogError("Max iterations reached trying to find a unit that can act!");
                }
                
                // If game ended during increment loop, stop
                if (gameEnded)
                {
                    return;
                }
            }
        }
        
        string gaugeInfo = gaugeBeforeReset > 100f 
            ? $"Gauge {gaugeBeforeReset:F1} -> {gaugeAfterReset:F1} (preserved excess)" 
            : $"Gauge reset to {gaugeAfterReset:F1}";
        Debug.Log(currentUnit.gameObject.name + " finished their turn. " + gaugeInfo + ". All other units' gauges incremented.");
        
        // Now select the next unit that can act
        SelectNextUnitToAct();
    }
    
    /// <summary>
    /// Returns whether the game has ended
    /// Used by TurnOrderTimeline to know when to stop displaying turn order
    /// </summary>
    public bool IsGameEnded()
    {
        return gameEnded;
    }
}
