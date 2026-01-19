using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Map;

public class LevelMap : MonoBehaviour
{
    [Header("Map System")]
    [Tooltip("Reference to MapManager for the new map system")]
    public MapManager mapManager;
    
    [Header("Level Data B1")]
    [Tooltip("List of LevelData ScriptableObjects that can be selected from the map")]
    public List<LevelData> levelDataList = new List<LevelData>();
    
    [Header("Level Data B2")]
    [Tooltip("List of LevelData ScriptableObjects for B2 that can be selected from the map")]
    public List<LevelData> levelDataListB2 = new List<LevelData>();
    
    [Header("Level Data B3")]
    [Tooltip("List of LevelData ScriptableObjects for B3 that can be selected from the map")]
    public List<LevelData> levelDataListB3 = new List<LevelData>();
    
    [Header("Level Data Boss")]
    [Tooltip("Boss LevelData ScriptableObject")]
    public LevelData bossLevelData;
    
    #region Lifecycle Methods
    
    void Start()
    {
        // Show map panel at start
        ShowMapOnStart();
        
        // Hide all UI when map is open - use coroutine to ensure GameManager is ready
        StartCoroutine(HideAllUIWhenReady());
        
        // Disable TurnOrder selection until a level is selected
        DisableTurnSelectionUntilChoice();
    }
    
    #endregion
    
    #region Initialization Helpers
    
    /// <summary>
    /// Shows the map at start using MapView/MapManager system
    /// MapManager may have already shown it in Start(), so we just ensure it's visible
    /// </summary>
    private void ShowMapOnStart()
    {
        // Wait a frame to ensure MapManager has initialized
        StartCoroutine(ShowMapViewDelayed());
    }
    
    /// <summary>
    /// Coroutine to show map view after MapManager has initialized
    /// </summary>
    private IEnumerator ShowMapViewDelayed()
    {
        yield return null; // Wait one frame for MapManager.Start() to complete
        ShowMapView();
    }
    
    /// <summary>
    /// Disables TurnOrder selection until a level is selected
    /// </summary>
    private void DisableTurnSelectionUntilChoice()
    {
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
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Shows the map when a stage is completed or when choosing a stage to navigate to
    /// Uses the MapView/MapManager system instead of the old mapPanel
    /// </summary>
    public void ShowMapPanel()
    {
        ShowMapView();
        
        // Hide all UI when map opens
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.HideAllUI();
        }
        
        // Don't advance stage here - stage advances after selecting from the map
        // This ensures the map shows the current stage (B1, B2, B3) before selection
    }
    
    /// <summary>
    /// Hides the map
    /// </summary>
    public void HideMapPanel()
    {
        HideMapView();
    }
    
    /// <summary>
    /// Shows the MapView map system
    /// </summary>
    private void ShowMapView()
    {
        // Find MapManager if not assigned
        if (mapManager == null)
        {
            mapManager = FindFirstObjectByType<MapManager>();
        }
        
        if (mapManager != null && mapManager.view != null)
        {
            // Ensure MapView GameObject is active
            if (!mapManager.view.gameObject.activeSelf)
            {
                mapManager.view.gameObject.SetActive(true);
            }
            
            // If map hasn't been shown yet, show it
            if (mapManager.CurrentMap != null)
            {
                mapManager.view.ShowMap(mapManager.CurrentMap);
            }
            else
            {
                // Generate a new map if none exists
                mapManager.GenerateNewMap();
            }
        }
        else
        {
            Debug.LogWarning("LevelMap: MapManager or MapView not found! Cannot show map.");
        }
        
        // Unlock node selection when map is shown again (after completing a level)
        Map.MapPlayerTracker playerTracker = FindFirstObjectByType<Map.MapPlayerTracker>();
        if (playerTracker != null)
        {
            playerTracker.UnlockNodeSelection();
        }
    }
    
    /// <summary>
    /// Hides the MapView map system
    /// </summary>
    private void HideMapView()
    {
        // Find MapManager if not assigned
        if (mapManager == null)
        {
            mapManager = FindFirstObjectByType<MapManager>();
        }
        
        if (mapManager != null && mapManager.view != null)
        {
            // Hide the MapView GameObject
            mapManager.view.gameObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public Level Start Methods
    
    /// <summary>
    /// Starts a level based on the node type from the map system
    /// Maps node types to appropriate level data lists based on current stage
    /// </summary>
    public void StartLevelFromNodeType(NodeType nodeType)
    {
        // Get current stage to determine which level list to use
        LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
        int currentStage = 0;
        if (levelNavigation != null)
        {
            currentStage = levelNavigation.GetCurrentStage();
        }
        
        LevelData selectedLevel = null;
        
        // Map node types to level data lists
        switch (nodeType)
        {
            case NodeType.Boss:
                // Boss always uses bossLevelData
                selectedLevel = bossLevelData;
                break;
                
            case NodeType.MinorEnemy:
            case NodeType.EliteEnemy:
            case NodeType.Mystery:
            case NodeType.RestSite:
            case NodeType.Treasure:
            case NodeType.Store:
                // All other node types use stage-appropriate level lists
                List<LevelData> levelList = GetLevelListForStage(currentStage);
                if (levelList != null && levelList.Count > 0)
                {
                    List<LevelData> validLevels = GetValidLevels(levelList);
                    if (validLevels.Count > 0)
                    {
                        // Select a random level from the list
                        selectedLevel = validLevels[Random.Range(0, validLevels.Count)];
                    }
                }
                break;
                
            default:
                Debug.LogWarning($"LevelMap: Unknown node type: {nodeType}");
                break;
        }
        
        if (selectedLevel != null)
        {
            Debug.Log($"LevelMap: Starting level from node type {nodeType}: {selectedLevel.levelID}");
            StartLevel(selectedLevel);
        }
        else
        {
            Debug.LogWarning($"LevelMap: Could not find a level for node type {nodeType} at stage {currentStage}");
        }
    }
    
    /// <summary>
    /// Gets the appropriate level list for the current stage
    /// </summary>
    private List<LevelData> GetLevelListForStage(int stage)
    {
        // Stage 0 = before B1 (use B1 list)
        // Stage 1 = after B1 (use B2 list)
        // Stage 2 = after B2 (use B3 list)
        // Stage 3+ = after B3 (use B3 list as fallback, or could use boss)
        switch (stage)
        {
            case 0:
                return levelDataList;
            case 1:
                return levelDataListB2;
            case 2:
            default:
                return levelDataListB3;
        }
    }
    
    #endregion
    
    #region Level Selection
    
    /// <summary>
    /// Gets valid (non-null) levels from the level data list
    /// </summary>
    private List<LevelData> GetValidLevels(List<LevelData> levelList)
    {
        List<LevelData> validLevels = new List<LevelData>();
        foreach (var levelData in levelList)
        {
            if (levelData != null)
            {
                validLevels.Add(levelData);
            }
        }
        return validLevels;
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
        
        // Hide the map
        HideMapPanel();
        
        // Show all UI elements (unhide everything for the first round)
        // Skip showing turn order timeline, status display panel, event panel, current level text, settings button, and spawn areas
        // They will be shown after initialization to prevent showing incorrect information and ensure everything appears together
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.ShowAllUI(showTurnOrderTimeline: false, showStatusDisplayPanel: false, showEventPanel: false, showCurrentLevelText: false, showCurrentGoldText: false, showSettingsButton: false, showSpawnAreas: false);
        }
        
        // Start coroutine to handle level start with proper timing
        StartCoroutine(StartLevelCoroutine(levelData));
    }
    
    #endregion
    
    #region Level Start Coroutine
    
    /// <summary>
    /// Coroutine to handle level start with proper timing
    /// </summary>
    private IEnumerator StartLevelCoroutine(LevelData levelData)
    {
        // Wait a short moment to ensure all previous operations have completed
        yield return new WaitForSeconds(0.2f);
        
        // Cache all component references
        ComponentReferences refs = CacheComponentReferences();
        
        // Pause turn selection while resetting to prevent early picks
        PauseTurnSelection(refs.turnOrder);
        
        // Reset all managers (GameManager, TurnOrder, EventLogPanel, ActionPanelManager)
        ResetManagers(refs);
        
        // Clear existing units and spawn new ones
        ClearAndSpawnUnits(refs.spawning, levelData);
        
        // Update level display
        UpdateLevelDisplay(refs.levelNavigation, levelData);
        
        // Reinitialize gauges and select first unit
        if (refs.gameManager != null)
        {
            yield return StartCoroutine(ReinitializeGaugesAndSelectFirstUnit(refs.gameManager, refs.turnOrder));
        }

        // Resume turn selection after reinitialization
        ResumeTurnSelection(refs.turnOrder);
        
        // Show remaining UI elements together after initialization is complete
        ShowRemainingUIElements(refs.gameManager);
    }
    
    /// <summary>
    /// Caches all component references needed for level start
    /// </summary>
    private ComponentReferences CacheComponentReferences()
    {
        return new ComponentReferences
        {
            turnOrder = FindFirstObjectByType<TurnOrder>(),
            eventLogPanel = FindFirstObjectByType<EventLogPanel>(),
            spawning = FindFirstObjectByType<Spawning>(),
            gameManager = FindFirstObjectByType<GameManager>(),
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>(),
            levelNavigation = FindFirstObjectByType<LevelNavigation>()
        };
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
        public LevelNavigation levelNavigation;
    }
    
    /// <summary>
    /// Pauses turn selection
    /// </summary>
    private void PauseTurnSelection(TurnOrder turnOrder)
    {
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(false);
        }
    }
    
    /// <summary>
    /// Resumes turn selection
    /// </summary>
    private void ResumeTurnSelection(TurnOrder turnOrder)
    {
        if (turnOrder != null)
        {
            turnOrder.SetSelectionEnabled(true);
        }
    }
    
    /// <summary>
    /// Resets all managers (GameManager, TurnOrder, EventLogPanel, ActionPanelManager)
    /// </summary>
    private void ResetManagers(ComponentReferences refs)
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
    /// Clears existing units and spawns new ones
    /// </summary>
    private void ClearAndSpawnUnits(Spawning spawning, LevelData levelData)
    {
        if (spawning != null)
        {
            // Clear all existing units (both enemies and allies)
            spawning.ClearAllSpawnedUnits();
            
            // Spawn allies and enemies at the same time
            spawning.SpawnCreaturesOnly();
            spawning.SpawnEnemiesForLevel(levelData);
        }
    }
    
    /// <summary>
    /// Updates level display in LevelNavigation
    /// </summary>
    private void UpdateLevelDisplay(LevelNavigation levelNavigation, LevelData levelData)
    {
        // Update LevelNavigation if it exists
        // Note: We don't change the stage here - stage only advances when map panel opens after completing a stage
        // The display will show the current stage (B1, B2, B3, etc.) regardless of which level was selected
        if (levelNavigation != null && !string.IsNullOrEmpty(levelData.levelID))
        {
            levelNavigation.SetCurrentLevel(levelData.levelID);
            // Update display to show current stage (B1, B2, B3, etc.)
            levelNavigation.UpdateLevelDisplay();
        }
    }
    
    /// <summary>
    /// Shows remaining UI elements after initialization
    /// </summary>
    private void ShowRemainingUIElements(GameManager gameManager)
    {
        if (gameManager != null)
        {
            // Show status display panel (contains creature and enemy UI roots)
            gameManager.ShowStatusDisplayPanel();
            // Update all unit UI to ensure correct information is displayed
            gameManager.UpdateAllUnitUI();
            // Show event panel (log is already cleared, so it will be empty)
            gameManager.ShowEventPanel();
            // Show player info panel (contains current level text and current gold text) and settings button
            gameManager.ShowPlayerInfo();
            gameManager.ShowSettingsButton();
            // Then show turn order timeline
            gameManager.ShowTurnOrderTimeline();
        }
    }
    
    #endregion
    
    #region Reinitialization
    
    /// <summary>
    /// Coroutine to reinitialize the game after starting a new level
    /// </summary>
    private IEnumerator ReinitializeGaugesAndSelectFirstUnit(GameManager gameManager, TurnOrder turnOrder)
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
            ResetAllUnitGauges();
            
            // Increment all units' gauges until someone reaches 100 for the first turn
            IncrementGaugesUntilSomeoneCanAct();
            
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
    
    /// <summary>
    /// Resets all unit gauges to 0
    /// </summary>
    private void ResetAllUnitGauges()
    {
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
    }
    
    /// <summary>
    /// Increments all units' gauges until someone reaches 100
    /// </summary>
    private void IncrementGaugesUntilSomeoneCanAct()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            return;
        }
        
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
    
    #endregion
}
