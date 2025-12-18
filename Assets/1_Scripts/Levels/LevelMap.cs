using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LevelMap : MonoBehaviour
{
    [Header("Map Panel")]
    [Tooltip("The map panel GameObject that will be shown when a stage is completed")]
    public GameObject mapPanel;
    
    [Header("Map Buttons B1-1")]
    [Tooltip("First button on the map panel")]
    public Button mapButton1;
    
    [Tooltip("Second button on the map panel")]
    public Button mapButton2;
    
    [Tooltip("Third button on the map panel")]
    public Button mapButton3;
    
    [Header("Level Data B1")]
    [Tooltip("List of LevelData ScriptableObjects that can be selected from the map")]
    public List<LevelData> levelDataList = new List<LevelData>();
    
    void Start()
    {
        // Show map panel at start
        if (mapPanel != null)
        {
            mapPanel.SetActive(true);
        }
        
        // Hide all UI when map is open - use coroutine to ensure GameManager is ready
        StartCoroutine(HideAllUIWhenReady());
        
        // Set up button listeners
        if (mapButton1 != null)
        {
            mapButton1.onClick.AddListener(() => OnMapButtonClicked());
        }
        if (mapButton2 != null)
        {
            mapButton2.onClick.AddListener(() => OnMapButtonClicked());
        }
        if (mapButton3 != null)
        {
            mapButton3.onClick.AddListener(() => OnMapButtonClicked());
        }
        
        // Disable TurnOrder selection until a level is selected
        TurnOrder turnOrder = FindFirstObjectByType<TurnOrder>();
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(false);
        }
    }
    
    /// <summary>
    /// Coroutine to hide all UI once GameManager is ready
    /// </summary>
    private IEnumerator HideAllUIWhenReady()
    {
        // Wait a frame to ensure GameManager is initialized
        yield return null;
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            // Ensure GameManager is active
            if (!gameManager.gameObject.activeSelf)
            {
                gameManager.gameObject.SetActive(true);
            }
            
            // Hide all UI elements
            gameManager.HideAllUI();
        }
    }
    
    /// <summary>
    /// Shows the map panel when a stage is completed or when choosing a stage to navigate to
    /// </summary>
    public void ShowMapPanel()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("LevelMap: mapPanel is not assigned!");
        }
        
        // Hide all UI when map panel opens
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.HideAllUI();
        }
        
        // Don't advance stage here - stage advances after selecting from the map
        // This ensures the map shows the current stage (B1, B2, B3) before selection
    }
    
    /// <summary>
    /// Hides the map panel
    /// </summary>
    public void HideMapPanel()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when any map button is clicked - selects a random level and starts it
    /// </summary>
    private void OnMapButtonClicked()
    {
        // Select a random LevelData from the list
        if (levelDataList == null || levelDataList.Count == 0)
        {
            Debug.LogWarning("LevelMap: levelDataList is empty! Cannot select a level.");
            return;
        }
        
        // Filter out null entries
        List<LevelData> validLevels = new List<LevelData>();
        foreach (var levelData in levelDataList)
        {
            if (levelData != null)
            {
                validLevels.Add(levelData);
            }
        }
        
        if (validLevels.Count == 0)
        {
            Debug.LogWarning("LevelMap: No valid LevelData in levelDataList! Cannot select a level.");
            return;
        }
        
        // Select a random level
        LevelData selectedLevel = validLevels[Random.Range(0, validLevels.Count)];
        
        Debug.Log($"LevelMap: Selected random level: {selectedLevel.levelID}");
        
        // Start the level
        StartLevel(selectedLevel);
    }
    
    /// <summary>
    /// Starts a level by clearing enemies, spawning new ones, and resetting game state
    /// </summary>
    private void StartLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogWarning("LevelMap: Cannot start level - LevelData is null!");
            return;
        }
        
        // Advance to next stage when selecting from the map (B1 -> B2 -> B3, etc.)
        // This happens BEFORE starting the level so the display shows the correct stage
        LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
        if (levelNavigation != null)
        {
            levelNavigation.AdvanceToNextStage();
        }
        
        // Hide the map panel
        HideMapPanel();
        
        // Show all UI elements (unhide everything for the first round)
        // Skip showing turn order timeline, status display panel, event panel, current level text, settings button, and spawn areas
        // They will be shown after initialization to prevent showing incorrect information and ensure everything appears together
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ShowAllUI(showTurnOrderTimeline: false, showStatusDisplayPanel: false, showEventPanel: false, showCurrentLevelText: false, showSettingsButton: false, showSpawnAreas: false);
        }
        
        // Start coroutine to handle level start with proper timing
        StartCoroutine(StartLevelCoroutine(levelData));
    }
    
    /// <summary>
    /// Coroutine to handle level start with proper timing
    /// </summary>
    private IEnumerator StartLevelCoroutine(LevelData levelData)
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
        
        // Clear existing enemies and allies, then spawn both at the same time
        if (spawning != null)
        {
            // Clear enemy spawn areas
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
            
            // Clear creature spawn areas (allies)
            for (int i = 0; i < spawning.creatureSpawnAreas.Length; i++)
            {
                if (spawning.creatureSpawnAreas[i] != null)
                {
                    // Destroy all children (allies) in creature spawn areas
                    for (int c = spawning.creatureSpawnAreas[i].childCount - 1; c >= 0; c--)
                    {
                        Destroy(spawning.creatureSpawnAreas[i].GetChild(c).gameObject);
                    }
                }
            }
            
            // Spawn allies and enemies at the same time
            spawning.SpawnCreaturesOnly();
            spawning.SpawnEnemiesForLevel(levelData);
        }
        
        // Update LevelNavigation if it exists
        // Note: We don't change the stage here - stage only advances when map panel opens after completing a stage
        // The display will show the current stage (B1, B2, B3, etc.) regardless of which level was selected
        LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
        if (levelNavigation != null && !string.IsNullOrEmpty(levelData.levelID))
        {
            levelNavigation.SetCurrentLevel(levelData.levelID);
            // Update display to show current stage (B1, B2, B3, etc.)
            levelNavigation.UpdateLevelDisplay();
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
        
        // Show remaining UI elements together after initialization is complete
        // Spawn areas are already shown in ReinitializeGame() so units are active
        // This prevents UI from showing incorrect information before units are initialized
        if (gameManager != null)
        {
            // Show status display panel (contains creature and enemy UI roots)
            gameManager.ShowStatusDisplayPanel();
            // Update all unit UI to ensure correct information is displayed
            gameManager.UpdateAllUnitUI();
            // Show event panel (log is already cleared, so it will be empty)
            gameManager.ShowEventPanel();
            // Show current level text and settings button
            gameManager.ShowCurrentLevelText();
            gameManager.ShowSettingsButton();
            // Then show turn order timeline
            gameManager.ShowTurnOrderTimeline();
        }
    }
    
    /// <summary>
    /// Coroutine to reinitialize the game after starting a new level
    /// </summary>
    private IEnumerator ReinitializeGame(GameManager gameManager, TurnOrder turnOrder)
    {
        // Wait multiple frames to ensure all units are fully initialized and destroyed units are cleaned up
        yield return null;
        yield return null;
        
        // Show spawn areas first so units are active and can be found
        // This must happen before initializing gauges, otherwise FindObjectsByType won't find inactive units
        if (gameManager != null)
        {
            gameManager.ShowSpawnAreas();
        }
        
        // Wait one more frame to ensure units are active
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
            else
            {
                Debug.LogWarning("LevelMap: No units found after spawning! Units may not be active.");
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
                    Debug.LogError("Max iterations reached during first turn initialization after level start!");
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
                Debug.LogWarning("LevelMap: No first unit found after level start! All units may have gauge < 100.");
            }
        }
    }
}
