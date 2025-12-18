using UnityEngine;
using TMPro;

public class LevelNavigation : MonoBehaviour
{
    [Header("Level Display")]
    [Tooltip("Text that displays the current level")]
    public TextMeshProUGUI levelText;
    
    private string currentLevel = "B1-1";
    private int currentStage = 0; // Tracks which stage/floor we're on (0 = before B1, 1 = B1, 2 = B2, 3 = B3, etc.)
    
    // Start is called once before the first execution of Update after the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start at stage 0 (before B1) - will advance to B1 when first level is selected
        currentStage = 0;
        UpdateLevelDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Updates the level display text
    /// Shows the current stage as B1, B2, B3, etc.
    /// </summary>
    public void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            // Display current stage as B1, B2, B3, etc.
            // If stage is 0, show empty or "B0" (before first stage)
            if (currentStage > 0)
            {
                levelText.text = $"B{currentStage}";
            }
            else
            {
                levelText.text = ""; // Or "B0" if you want to show something before B1
            }
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
        // Don't change stage here - stage is only incremented when completing a level
        UpdateLevelDisplay();
    }
    
    /// <summary>
    /// Advances to the next stage (B1 -> B2 -> B3, etc.)
    /// Called when a level is completed
    /// </summary>
    public void AdvanceToNextStage()
    {
        currentStage++;
        UpdateLevelDisplay();
    }
    
    /// <summary>
    /// Gets the current stage number (1 = B1, 2 = B2, 3 = B3, etc.)
    /// </summary>
    public int GetCurrentStage()
    {
        return currentStage;
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
        
        // Cache component references
        ComponentReferences refs = CacheComponentReferences();
        
        // Pause turn selection while resetting to prevent early picks
        PauseTurnSelection(refs.turnOrder);
        
        // Reset combat and UI state
        ResetCombatAndUIState(refs);
        
        // Clear existing enemies and spawn next level enemies
        ClearEnemiesAndSpawnNextLevel(refs.spawning, nextLevel);
        
        // Update current level and hide round end panel
        UpdateLevelAndHideRoundEndPanel(nextLevel, refs.gameManager);
        
        // Reinitialize gauges and pick first unit
        yield return StartCoroutine(ReinitializeGaugesAndPickFirstUnit(refs.gameManager, refs.turnOrder));

        // Resume turn selection after reinitialization
        ResumeTurnSelection(refs.turnOrder);
    }
    
    /// <summary>
    /// Caches references to all necessary components
    /// </summary>
    private ComponentReferences CacheComponentReferences()
    {
        return new ComponentReferences
        {
            turnOrder = FindFirstObjectByType<TurnOrder>(),
            eventLogPanel = FindFirstObjectByType<EventLogPanel>(),
            spawning = FindFirstObjectByType<Spawning>(),
            gameManager = FindFirstObjectByType<GameManager>(),
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>()
        };
    }
    
    /// <summary>
    /// Pauses turn selection to prevent early picks during reset
    /// </summary>
    private void PauseTurnSelection(TurnOrder turnOrder)
    {
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(false);
        }
    }
    
    /// <summary>
    /// Resumes turn selection after reinitialization
    /// </summary>
    private void ResumeTurnSelection(TurnOrder turnOrder)
    {
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(true);
        }
    }
    
    /// <summary>
    /// Resets combat and UI state (TurnOrder, GameManager, panels, logs)
    /// </summary>
    private void ResetCombatAndUIState(ComponentReferences refs)
    {
        // Clear current unit in GameManager before resetting
        if (refs.gameManager != null)
        {
            refs.gameManager.ClearCurrentUnit();
        }
        
        // Reset TurnOrder
        if (refs.turnOrder != null)
        {
            refs.turnOrder.ResetGame();
        }
        
        // Clear EventLogPanel
        if (refs.eventLogPanel != null)
        {
            refs.eventLogPanel.ClearLog();
        }
        
        // Reset ActionPanelManager
        if (refs.actionPanelManager != null)
        {
            refs.actionPanelManager.Reset();
        }
    }
    
    /// <summary>
    /// Clears existing enemies and spawns enemies for the next level
    /// </summary>
    private void ClearEnemiesAndSpawnNextLevel(Spawning spawning, string nextLevel)
    {
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
    }
    
    /// <summary>
    /// Updates current level, display, and hides round end panel
    /// </summary>
    private void UpdateLevelAndHideRoundEndPanel(string nextLevel, GameManager gameManager)
    {
        // Update current level
        currentLevel = nextLevel;
        UpdateLevelDisplay();
        
        // Hide round end panel
        if (gameManager != null && gameManager.roundEndPanel != null)
        {
            gameManager.roundEndPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Coroutine to reinitialize gauges and pick the first unit
    /// </summary>
    private System.Collections.IEnumerator ReinitializeGaugesAndPickFirstUnit(GameManager gameManager, TurnOrder turnOrder)
    {
        // Wait multiple frames to ensure all units are fully initialized and destroyed units are cleaned up
        yield return null;
        yield return null;
        
        // Update all unit UI
        if (gameManager != null)
        {
            gameManager.UpdateAllUnitUI();
        }
        
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
    
    /// <summary>
    /// Helper class to hold component references
    /// </summary>
    private class ComponentReferences
    {
        public TurnOrder turnOrder;
        public EventLogPanel eventLogPanel;
        public Spawning spawning;
        public GameManager gameManager;
        public ActionPanelManager actionPanelManager;
    }
}
