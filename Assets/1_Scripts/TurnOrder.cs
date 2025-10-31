using UnityEngine;
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
    }

    /// <summary>
    /// Updates the highlight color/transparency on the current unit's spawn location capsule renderer (unit1-6)
    /// </summary>
    private void UpdateHighlight()
    {
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
    /// Gets the unit that should go first based on speed.
    /// If multiple units have the same highest speed, the one with the lowest spawn index goes first (deterministic).
    /// </summary>
    /// <returns>The Unit with the highest speed (deterministically chosen by spawn position if tied)</returns>
    public Unit GetFirstUnit()
    {
        // Get or calculate turn order
        List<Unit> turnOrder = GetTurnOrder();
        
        if (turnOrder == null || turnOrder.Count == 0)
        {
            Debug.LogWarning("No units found in turn order");
            return null;
        }

        // The first unit in the turn order is the one that should go first
        Unit firstUnit = turnOrder[0];
        
        // Log which unit is going first
        Debug.Log(firstUnit.gameObject.name + " goes first! (Speed: " + firstUnit.Speed + ")");
        
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
    /// Advances to the next unit's turn
    /// </summary>
    public void AdvanceToNextTurn()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("Cannot advance turn - GameManager not found!");
                return;
            }
        }

        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("Cannot advance turn - no current unit!");
            return;
        }

        // Get full turn order (use cached version for consistency)
        var turnOrderList = GetTurnOrder();
        if (turnOrderList == null || turnOrderList.Count == 0)
        {
            Debug.LogWarning("No units in turn order!");
            return;
        }

        // Find current unit's index
        int currentIndex = -1;
        for (int i = 0; i < turnOrderList.Count; i++)
        {
            if (turnOrderList[i] == currentUnit)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex == -1)
        {
            Debug.LogWarning("Current unit not found in turn order!");
            return;
        }

        // Move to next unit (wrap around if at end) - this ensures deterministic looping
        int nextIndex = (currentIndex + 1) % turnOrderList.Count;
        Unit nextUnit = turnOrderList[nextIndex];

        // Make sure the next unit is alive (skip dead units but maintain order)
        int attempts = 0;
        while (!nextUnit.IsAlive() && attempts < turnOrderList.Count)
        {
            nextIndex = (nextIndex + 1) % turnOrderList.Count;
            nextUnit = turnOrderList[nextIndex];
            attempts++;
        }

        if (nextUnit.IsAlive())
        {
            gameManager.SetCurrentUnit(nextUnit);
        }
        else
        {
            Debug.Log("No alive units left!");
        }
    }
}
