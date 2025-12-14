using UnityEngine;
using TMPro;

public class LevelNavigation : MonoBehaviour
{
    [Header("Level Display")]
    [Tooltip("Text that displays the current level")]
    public TextMeshProUGUI levelText;
    
    private string currentLevel = "B1-1";
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateLevelDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Updates the level display text
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = currentLevel;
        }
    }
    
    /// <summary>
    /// Gets the current level
    /// </summary>
    public string GetCurrentLevel()
    {
        return currentLevel;
    }
    
    /// <summary>
    /// Sets the current level and updates the display
    /// </summary>
    public void SetCurrentLevel(string level)
    {
        currentLevel = level;
        UpdateLevelDisplay();
    }
    
    /// <summary>
    /// Gets the next level after the current one
    /// Returns null if there is no next level
    /// </summary>
    public string GetNextLevel()
    {
        if (currentLevel == "B1-1")
        {
            return "B1-2";
        }
        else if (currentLevel == "B1-2")
        {
            return "B1-3";
        }
        else if (currentLevel == "B1-3")
        {
            // No more levels after B1-3 (for now)
            return null;
        }
        
        return null;
    }
    
    /// <summary>
    /// Advances to the next level: resets game state, clears enemies, spawns new enemies, and reinitializes
    /// </summary>
    public void AdvanceToNextLevel()
    {
        string nextLevel = GetNextLevel();
        if (string.IsNullOrEmpty(nextLevel))
        {
            Debug.LogWarning("No next level available!");
            return;
        }
        
        Debug.Log($"Advancing from {currentLevel} to {nextLevel}");
        
        // Start coroutine to handle level advancement with a short delay to ensure state has settled
        StartCoroutine(AdvanceToNextLevelCoroutine(nextLevel));
    }
    
    /// <summary>
    /// Coroutine to handle level advancement with proper timing
    /// </summary>
    private System.Collections.IEnumerator AdvanceToNextLevelCoroutine(string nextLevel)
    {
        // Wait a short moment to ensure all previous operations have completed
        yield return new WaitForSeconds(0.2f);
        
        // Get references to all necessary components
        TurnOrder turnOrder = FindFirstObjectByType<TurnOrder>();
        EventLogPanel eventLogPanel = FindFirstObjectByType<EventLogPanel>();
        Spawning spawning = FindFirstObjectByType<Spawning>();
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        ActionPanelManager actionPanelManager = FindFirstObjectByType<ActionPanelManager>();

        // Pause turn selection while resetting to prevent early picks
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(false);
        }
        
        // Clear current unit in GameManager before resetting
        if (gameManager != null)
        {
            gameManager.ClearCurrentUnit();
        }
        
        // Reset TurnOrder
        if (turnOrder != null)
        {
            turnOrder.ResetGame();
        }
        
        // Clear EventLogPanel
        if (eventLogPanel != null)
        {
            eventLogPanel.ClearLog();
        }
        
        // Reset ActionPanelManager
        if (actionPanelManager != null)
        {
            actionPanelManager.Reset();
        }
        
        // Clear existing enemies (but keep player units)
        if (spawning != null)
        {
            // Clear only enemy spawn areas
            for (int i = 0; i < spawning.enemySpawnAreas.Length; i++)
            {
                if (spawning.enemySpawnAreas[i] != null)
                {
                    // Destroy all children (enemies) in enemy spawn areas
                    for (int c = spawning.enemySpawnAreas[i].childCount - 1; c >= 0; c--)
                    {
                        Destroy(spawning.enemySpawnAreas[i].GetChild(c).gameObject);
                    }
                }
            }
            
            // Spawn enemies for the next level
            spawning.SpawnEnemiesForLevel(nextLevel);
        }
        
        // Update current level
        currentLevel = nextLevel;
        UpdateLevelDisplay();
        
        // Hide round end panel
        if (gameManager != null && gameManager.roundEndPanel != null)
        {
            gameManager.roundEndPanel.SetActive(false);
        }
        
        // Reinitialize the game (similar to GameManager.DelayedStart)
        if (gameManager != null)
        {
            yield return StartCoroutine(ReinitializeGame(gameManager, turnOrder));
        }

        // Resume turn selection after reinitialization
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(true);
        }
    }
    
    /// <summary>
    /// Coroutine to reinitialize the game after advancing to next level
    /// </summary>
    private System.Collections.IEnumerator ReinitializeGame(GameManager gameManager, TurnOrder turnOrder)
    {
        // Wait multiple frames to ensure all units are fully initialized and destroyed units are cleaned up
        yield return null;
        yield return null;
        
        // Update all unit UI
        gameManager.UpdateAllUnitUI();
        
        if (turnOrder != null)
        {
            // Reset all unit gauges to 0 before incrementing
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allUnits != null && allUnits.Length > 0)
            {
                foreach (var unit in allUnits)
                {
                    if (unit != null && unit.IsAlive())
                    {
                        unit.ResetActionGaugeHard();
                    }
                }
            }
            
            // Increment all units' gauges until someone reaches 100 for the first turn
            if (allUnits != null && allUnits.Length > 0)
            {
                bool someoneCanAct = false;
                int maxIterations = 20; // Safety limit
                int iterations = 0;
                
                while (!someoneCanAct && iterations < maxIterations)
                {
                    iterations++;
                    
                    foreach (var unit in allUnits)
                    {
                        if (unit == null || !unit.IsAlive())
                            continue;
                        
                        bool reached100 = unit.IncrementActionGauge();
                        
                        if (reached100)
                        {
                            someoneCanAct = true;
                        }
                    }
                }
                
                if (iterations >= maxIterations && !someoneCanAct)
                {
                    Debug.LogError("Max iterations reached during first turn initialization after level advance!");
                }
            }
            
            // Wait a moment for initialization
            yield return new WaitForSeconds(0.1f);
            
            // Get the first unit with tiebreaker logic
            Unit firstUnit = turnOrder.GetFirstUnit();
            if (firstUnit != null)
            {
                // Set the acting flag before setting the unit
                turnOrder.SetUnitActing(true);
                gameManager.SetCurrentUnit(firstUnit);
                // If first unit is enemy, delay processing slightly
                if (firstUnit.IsEnemyUnit)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                Debug.LogWarning("LevelNavigation: No first unit found after level advance! All units may have gauge < 100.");
            }
        }
    }
}
