using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TurnOrder turnOrder;
    
    [Tooltip("Reference to the Inventory component for currency tracking")]
    private Inventory inventory;

	[Header("Creature Status UI (3 units)")]
    [Tooltip("Creature name text displays (index 0-2 correspond to creature 1-3)")]
    public TextMeshProUGUI[] creatureNameTexts = new TextMeshProUGUI[3];
    
    [Tooltip("Creature health bar fill images (index 0-2 correspond to creature 1-3)")]
    public UnityEngine.UI.Image[] creatureHealthFills = new UnityEngine.UI.Image[3];
    
    [Tooltip("Creature action gauge fill images (index 0-2 correspond to creature 1-3)")]
    public UnityEngine.UI.Image[] creatureActionGaugeFills = new UnityEngine.UI.Image[3];
    
    [Header("Enemy Status UI (3 units)")]
    [Tooltip("Enemy name text displays (index 0-2 correspond to enemy 1-3)")]
    public TextMeshProUGUI[] enemyNameTexts = new TextMeshProUGUI[3];
    
    [Tooltip("Enemy health bar fill images (index 0-2 correspond to enemy 1-3)")]
    public UnityEngine.UI.Image[] enemyHealthFills = new UnityEngine.UI.Image[3];
    
    [Tooltip("Enemy action gauge fill images (index 0-2 correspond to enemy 1-3)")]
    public UnityEngine.UI.Image[] enemyActionGaugeFills = new UnityEngine.UI.Image[3];
    
    [Header("Name Text Font Size Settings")]
    [Tooltip("Maximum font size for short unit names")]
    [SerializeField] private float maxFontSize = 36f;
    
    [Tooltip("Minimum font size for very long unit names")]
    [SerializeField] private float minFontSize = 18f;
    
    [Tooltip("Name length at which font size starts scaling down (names this length or shorter use max font size)")]
    [SerializeField] private int shortNameThreshold = 10;
    
    [Tooltip("Name length at which font size reaches minimum (names this length or longer use min font size)")]
    [SerializeField] private int longNameThreshold = 30;
    
    [Header("Unit Name Colors")]
    [Tooltip("Color for ally/player unit names")]
    [SerializeField] private Color allyColor = Color.cyan;
    
    [Tooltip("Color for enemy unit names")]
    [SerializeField] private Color enemyColor = Color.red;
    
    // Static color storage for use in static methods
    private static Color staticAllyColor = Color.cyan;
    private static Color staticEnemyColor = Color.red;
    
    // Static instance for color access
    private static GameManager staticInstance;
    
    [Header("Unit UI Roots (toggle on death)")]
    [Tooltip("Root GameObjects for creature UI slots (0-2). These will be disabled when the unit dies.")]
    public GameObject[] creatureUIRoots = new GameObject[3];
    
    [Tooltip("Root GameObjects for enemy UI slots (0-2). These will be disabled when the unit dies.")]
    public GameObject[] enemyUIRoots = new GameObject[3];

	 [Header("User Panel UI Root")]
    public GameObject userPanelRoot;
	
	[Header("Status Display Panel")]
	[Tooltip("The status display panel that contains creature and enemy UI roots")]
	public GameObject statusDisplayPanel;
	
	[Header("Spawn Areas")]
	[Tooltip("Spawn area for creature units (player units) - will be hidden at start")]
	public Transform creatureSpawnArea;
	
	[Tooltip("Spawn area for enemy units - will be hidden at start")]
	public Transform enemySpawnArea;
	
	[Header("UI Panels")]
	[Tooltip("The main gameplay UI parent that contains statusDisplayPanel, turnOrderTimeline, eventPanel, infoPanel, and playerInfo - will be hidden at start")]
	public GameObject gameplayUI;
	
	[Tooltip("The TurnOrderTimeline GameObject")]
	public GameObject turnOrderTimeline;
	
	[Tooltip("The EventPanel GameObject")]
	public GameObject eventPanel;
	
	[Tooltip("The InfoPanel GameObject")]
	public GameObject infoPanel;
	
	[Header("PlayerInfo")]
	[Tooltip("The PlayerInfo GameObject parent that contains currentLevelText and currentGoldText - will be hidden at start")]
	public GameObject playerInfo;
	
	[Tooltip("The current level text GameObject - will be hidden at start")]
	public GameObject currentLevelText;
	
	[Tooltip("Text that displays the current gold/currency amount")]
	public TextMeshProUGUI currentGoldText;
	
	[Tooltip("Text that displays the current class")]
	public TextMeshProUGUI currentClassText;
	
	[Tooltip("Text that displays the player name")]
	public TextMeshProUGUI playerNameText;
	
	[Tooltip("Starting gold/currency amount (used when no saved data exists)")]
	[SerializeField] private int startingGold = 0;
	
	[Tooltip("Gold amount awarded after completing each floor (boss defeated)")]
	[SerializeField] private int goldPerWin = 0;
	
	/// <summary>
	/// Gets the starting gold amount (used by Inventory)
	/// </summary>
	public int StartingGold => startingGold;
	
	[Header("Other UI Elements")]
	[Tooltip("The settings button GameObject - will be hidden at start")]
	public GameObject settingsButton;
	
	[Header("Round End Panel")]
	[Tooltip("Panel that opens when the round ends (either all allies dead or all enemies dead)")]
	public GameObject roundEndPanel;
	
	[Tooltip("Text that displays either 'Game Over!' or 'Round Won!'")]
	public TextMeshProUGUI roundEndMessageText;
	
	[Tooltip("Button on the round end panel that returns to main menu")]
	public Button returnToMainMenuButton;
	
	[Tooltip("Button on the round end panel that restarts the round (reloads scene)")]
	public Button restartRoundButton;
	
	[Header("Level Navigation")]
	[Tooltip("Button to advance to the next level")]
	public Button nextLevelButton;
	
	[Tooltip("Name of the main menu scene to load")]
	public string mainMenuSceneName = "MainMenu";

    private TurnOrder turnOrderRef; // Reference to get spawn indices

    [Header("Turn Management")]
    private Unit currentUnit;
    
    [Header("Unit Highlighting")]
    [Tooltip("Enable/disable unit highlighting during their turn")]
    public bool enableUnitHighlighting = true;
    
    [Tooltip("Shared AllIn1SpriteShader material to use for highlighting units during their turn")]
    public Material highlightMaterial;
    
    // Cache the ActionPanelManager reference
    private ActionPanelManager actionPanelManager;
    
    [Header("Enemy Turn Settings")]
    [Tooltip("Delay in seconds before advancing to next turn after enemy completes their action")]
    [Range(0f, 5f)]
    public float enemyTurnDelay = 2f;
    
    [Header("Battle Animation Timing")]
    [Tooltip("Delay in seconds for skill animation to play (between skill usage message and damage message)")]
    [Range(0f, 5f)]
    public float skillAnimationDelay = 2f;
    
    [Tooltip("Additional delay in seconds after attack animation ends")]
    [Range(0f, 2f)]
    public float postAttackAnimationDelay = 0.2f;
    
    [Tooltip("Delay in seconds for hit animation to play (between damage message and turn advancement)")]
    [Range(0f, 5f)]
    public float hitAnimationDelay = 2f;
    
    [Tooltip("Additional delay in seconds before showing round won/game over screen (added on top of postAttackAnimationDelay)")]
    [Range(0f, 5f)]
    public float roundEndScreenDelay = 0.5f;
    
    [Tooltip("Duration in seconds for unit sprite fade out after death animation completes")]
    [Range(0f, 10f)]
    public float deathFadeOutDuration = 2f;

    #region Lifecycle Methods
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Set static instance and colors
        staticInstance = this;
        staticAllyColor = allyColor;
        staticEnemyColor = enemyColor;
        
        // Validate and cache references
        ValidateReferences();
        
        // Set up button listeners
        SetupButtonListeners();
        
        // Set up inventory and currency display
        SetupInventory();
        
        // Update player name text with current profile name
        UpdatePlayerNameText();
        
        // Hide round end panel at start
        if (roundEndPanel != null)
        {
            roundEndPanel.SetActive(false);
        }

        // Get the first unit that should go - delay slightly to ensure all units are initialized
        StartCoroutine(DelayedStart());
    }
    
    /// <summary>
    /// Gets the ally color for unit names (static access)
    /// </summary>
    public static Color GetAllyColor()
    {
        return staticAllyColor;
    }
    
    /// <summary>
    /// Gets the enemy color for unit names (static access)
    /// </summary>
    public static Color GetEnemyColor()
    {
        return staticEnemyColor;
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        // Wait a frame to ensure all units are fully initialized
        yield return null;
        
        // Cache TurnOrder reference for spawn index lookup
        CacheTurnOrderReference();
        
        // Connect all units to their UI elements
        UpdateAllUnitUI();
        
        // Initialize gauges and select first unit
        yield return StartCoroutine(InitializeGaugesAndSelectFirstUnit());
    }
    
    /// <summary>
    /// Cleanup: Unsubscribe from events when destroyed
    /// </summary>
    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnCurrencyChanged -= UpdateGoldText;
        }
    }

    // Update is called once per frame
    void Update()
    {
		// Debug: Print action gauge status when pressing G
		if (Keyboard.current != null && Keyboard.current[Key.G].wasPressedThisFrame)
		{
			PrintActionGaugeStatus();
		}
		
		// Update all action gauge UI displays
		UpdateAllActionGaugeUI();
    }
    
    #endregion
    
    #region Reference Validation
    
    /// <summary>
    /// Validates and caches all required references
    /// </summary>
    private void ValidateReferences()
    {
        // Find TurnOrder if not assigned
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
        }
        
        // Cache ActionPanelManager reference
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        }
    }
    
    /// <summary>
    /// Sets up inventory reference and subscribes to currency changes
    /// </summary>
    private void SetupInventory()
    {
        // Find Inventory component
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
        
        // Subscribe to currency changes
        if (inventory != null)
        {
            inventory.OnCurrencyChanged += UpdateGoldText;
            // Initialize gold text with current currency - use coroutine to ensure Inventory has loaded
            StartCoroutine(DelayedUpdateGoldText());
        }
        else
        {
            Debug.LogWarning("GameManager: Inventory component not found! Gold text will not update.");
        }
    }
    
    /// <summary>
    /// Coroutine to update gold text after Inventory has loaded (ensures starting gold is displayed)
    /// </summary>
    private System.Collections.IEnumerator DelayedUpdateGoldText()
    {
        // Wait a frame to ensure Inventory.Start() has been called and currency is loaded
        yield return null;
        
        if (inventory != null)
        {
            UpdateGoldText(inventory.CurrentGold);
        }
    }
    
    /// <summary>
    /// Updates the gold text display with the current currency amount
    /// </summary>
    private void UpdateGoldText(int currencyAmount)
    {
        if (currentGoldText != null)
        {
            currentGoldText.text = currencyAmount.ToString();
        }
    }
    
    /// <summary>
    /// Updates the player name text display with the current profile name
    /// </summary>
    private void UpdatePlayerNameText()
    {
        if (playerNameText != null)
        {
            string profileName = SaveProfiles.ProfileName;
            // If no profile name exists, use "guest" as fallback
            if (string.IsNullOrEmpty(profileName))
            {
                profileName = "guest";
            }
            playerNameText.text = profileName;
        }
    }
    
    /// <summary>
    /// Awards gold when a round is won (every round win)
    /// </summary>
    private void AwardFloorCompletionGold()
    {
        if (goldPerWin > 0)
        {
            // Ensure inventory reference is set
            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }
            
            if (inventory != null)
            {
                inventory.AddCurrency(goldPerWin);
                Debug.Log($"Awarded {goldPerWin} gold for winning round. New total: {inventory.CurrentGold}");
            }
            else
            {
                Debug.LogWarning("GameManager: Cannot award gold - Inventory component not found!");
            }
        }
    }
    
    /// <summary>
    /// Caches TurnOrder reference for spawn index lookup
    /// </summary>
    private void CacheTurnOrderReference()
    {
        if (turnOrder != null)
        {
            turnOrderRef = turnOrder;
        }
        else
        {
            turnOrderRef = FindFirstObjectByType<TurnOrder>();
        }
    }
    
    /// <summary>
    /// Sets up button listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        // Set up return to main menu button listener
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }
		
		// Set up restart round button listener
		if (restartRoundButton != null)
		{
			restartRoundButton.onClick.AddListener(OnRestartRoundClicked);
		}
		
		// Set up next level button listener
		if (nextLevelButton != null)
		{
			nextLevelButton.onClick.AddListener(OnNextLevelClicked);
		}
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Initializes gauges and selects the first unit to act
    /// </summary>
    private System.Collections.IEnumerator InitializeGaugesAndSelectFirstUnit()
    {
        if (turnOrder == null)
        {
            Debug.LogWarning("GameManager: No TurnOrder component found in scene!");
            yield break;
        }
        
        // Increment all units' gauges until someone reaches 100 for the first turn
        InitializeFirstTurnGauges();
        
        // Wait a moment for initialization
        yield return new WaitForSeconds(0.1f);
        
        // Get the first unit with tiebreaker logic (player units first, then by spawn slot)
        Unit firstUnit = turnOrder.GetFirstUnit();
        if (firstUnit != null)
        {
            //Debug.Log("GameManager: First unit determined - " + firstUnit.gameObject.name + " with gauge " + firstUnit.GetActionGauge());
            // Set the acting flag before setting the unit
            if (turnOrder != null)
            {
                turnOrder.SetUnitActing(true);
            }
            SetCurrentUnit(firstUnit);
            // If first unit is enemy, delay processing slightly to ensure player units exist
            if (firstUnit.IsEnemyUnit)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
        {
            // No units found - this is expected when units are hidden at start (map panel open)
            // Units will be found once a level is selected and started
        }
    }
    
    /// <summary>
    /// Initializes first turn by incrementing gauges until someone can act
    /// </summary>
    private void InitializeFirstTurnGauges()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allUnits == null || allUnits.Length == 0)
        {
            return;
        }
        
        bool someoneCanAct = false;
        int maxIterations = 20; // Safety limit
        int iterations = 0;
        
        //Debug.Log("=== Initializing first turn - incrementing gauges until someone can act ===");
        
        while (!someoneCanAct && iterations < maxIterations)
        {
            iterations++;
            
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive())
                    continue;
                
                float oldGauge = unit.GetActionGauge();
                bool reached100 = unit.IncrementActionGauge();
                float newGauge = unit.GetActionGauge();
                
                //Debug.Log($"{unit.gameObject.name} (Speed {unit.Speed}): Gauge {oldGauge:F1} -> {newGauge:F1}");
                
                if (reached100)
                {
                    someoneCanAct = true;
                }
            }
        }
        
        if (iterations >= maxIterations && !someoneCanAct)
        {
            Debug.LogError("Max iterations reached during first turn initialization!");
        }
    }
    
    #endregion
    
    #region Turn Management
    
    /// <summary>
    /// Sets the current unit whose turn it is and updates the UI
    /// </summary>
	public void SetCurrentUnit(Unit unit)
	{
		// Prevent duplicate calls for the same unit
		if (currentUnit == unit)
		{
			return;
		}
		
		// Safety check: don't set a dead unit as current
		if (unit != null && !unit.IsAlive())
		{
			Debug.LogWarning($"Attempted to set dead unit {unit.gameObject.name} as current unit! Skipping.");
			// Clear current unit and let TurnOrder select a new one
			currentUnit = null;
			if (turnOrder != null)
			{
				turnOrder.SetUnitActing(false);
			}
			return;
		}
		
		currentUnit = unit;
		UpdateSkillPanel();
		
		// If it's an enemy's turn (based on spawn area assignment), automatically process their turn
		if (currentUnit != null && currentUnit.IsEnemyUnit)
		{
			ProcessEnemyTurn();
		}
		else if (currentUnit != null && currentUnit.IsPlayerUnit)
		{
			// Player unit's turn started, call StartTurn to reduce cooldowns
			currentUnit.StartTurn();
		}
	}
	
	/// <summary>
	/// Gets the current unit whose turn it is
	/// </summary>
	public Unit GetCurrentUnit()
	{
		return currentUnit;
	}
	
	/// <summary>
	/// Clears the current unit (used when resetting for next level)
	/// </summary>
	public void ClearCurrentUnit()
	{
		currentUnit = null;
	}
	
	/// <summary>
	/// Calls TurnOrder to advance to the next turn
	/// </summary>
	private void AdvanceToNextTurn()
	{
		if (turnOrder == null)
		{
			turnOrder = FindFirstObjectByType<TurnOrder>();
		}
		
		if (turnOrder != null)
		{
			turnOrder.AdvanceToNextTurn();
		}
	}
	
	#endregion
    
    #region Enemy Turn Processing
    
    /// <summary>
    /// Processes an enemy's turn: picks random skill and targets random player unit
    /// </summary>
	private void ProcessEnemyTurn()
	{
		if (currentUnit == null || !currentUnit.IsEnemyUnit)
			return;
		
		// Check if the enemy unit itself is dead
		if (!currentUnit.IsAlive())
		{
			Debug.LogWarning($"Enemy unit {currentUnit.gameObject.name} is dead! Skipping turn and advancing.");
			AdvanceToNextTurn();
			return;
		}
		
		// Get all alive player units (targetable by enemies)
		List<Unit> playerUnits = GetAlivePlayerUnits();
		
		if (playerUnits.Count == 0)
		{
			Debug.LogWarning($"No alive player units to target! Advancing turn.");
			LogAllUnitsForDebug();
			AdvanceToNextTurn();
			return;
		}
		
		// Get available skills (not on cooldown)
		List<int> availableSkills = GetAvailableSkills(currentUnit);
		
		// If no skills available, just attack or skip
		if (availableSkills.Count == 0)
		{
			Debug.Log(currentUnit.gameObject.name + " has no available skills!");
			AdvanceToNextTurn();
			return;
		}
		
		// Pick random skill
		int randomSkillIndex = PickRandomSkill(availableSkills);
		
		// Pick random player unit target (already confirmed alive)
		Unit randomTarget = PickRandomTarget(playerUnits);
		
		// Double-check target is still alive (in case it died between selection and use)
		if (randomTarget == null || !randomTarget.IsAlive())
		{
			Debug.LogWarning($"Target {randomTarget?.gameObject.name} is dead or null! Finding another target...");
			// Filter to only alive targets again
			playerUnits.RemoveAll(u => u == null || !u.IsAlive());
			
			if (playerUnits.Count == 0)
			{
				Debug.LogWarning("No alive targets remaining! Advancing turn.");
				AdvanceToNextTurn();
				return;
			}
			
			randomTarget = PickRandomTarget(playerUnits);
		}
		
		// Start the turn (reduce cooldowns)
		currentUnit.StartTurn();
		
		// Hide action UI immediately to prevent input during skill execution
		HideActionUI();
		
		// Use the skill with delayed execution for proper animation timing
		StartCoroutine(ExecuteEnemySkillWithDelay(currentUnit, randomSkillIndex, randomTarget));
	}
	
	/// <summary>
	/// Gets all alive player units
	/// </summary>
	private List<Unit> GetAlivePlayerUnits()
	{
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		List<Unit> playerUnits = new List<Unit>();
		
		foreach (var unit in allUnits)
		{
			if (unit == null) continue;
			
			if (unit.IsPlayerUnit && unit.IsAlive())
			{
				playerUnits.Add(unit);
			}
		}
		
		return playerUnits;
	}
	
	/// <summary>
	/// Gets available skills (not on cooldown) for a unit
	/// </summary>
	private List<int> GetAvailableSkills(Unit unit)
	{
		List<int> availableSkills = new List<int>();
		for (int i = 0; i < unit.Skills.Length; i++)
		{
			if (unit.CanUseSkill(i))
			{
				availableSkills.Add(i);
			}
		}
		return availableSkills;
	}
	
	/// <summary>
	/// Picks a random skill from available skills
	/// </summary>
	private int PickRandomSkill(List<int> availableSkills)
	{
		return availableSkills[Random.Range(0, availableSkills.Count)];
	}
	
	/// <summary>
	/// Picks a random target from available targets
	/// </summary>
	private Unit PickRandomTarget(List<Unit> targets)
	{
		return targets[Random.Range(0, targets.Count)];
	}
	
	/// <summary>
	/// Logs all units for debugging
	/// </summary>
	private void LogAllUnitsForDebug()
	{
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		Debug.LogWarning($"Checking all units (Total: {allUnits.Length}):");
		foreach (var unit in allUnits)
		{
			if (unit != null)
			{
				Debug.LogWarning($"  - {unit.gameObject.name}: IsPlayerUnit={unit.IsPlayerUnit}, IsAlive={unit.IsAlive()}");
			}
		}
	}
	
	#endregion
    
    #region Skill Execution
    
    /// <summary>
    /// Public method to execute a skill with proper timing delays (for both player and enemy units)
    /// </summary>
	public void ExecuteSkillWithDelay(Unit caster, int skillIndex, Unit target, bool autoAdvanceTurn = false)
	{
		if (skillIndex < 0 || skillIndex >= caster.Skills.Length)
		{
			Debug.LogWarning($"Invalid skill index {skillIndex} for unit {caster.UnitName}");
			return;
		}
			
		Skill skill = caster.Skills[skillIndex];
		
		// Set cooldown (same as UseSkill does)
		caster.SetSkillCooldown(skillIndex, skill.cooldownTurns);
		
		// Hide action UI immediately to prevent input during skill execution
		HideActionUI();
		
		// Log skill usage immediately (before starting coroutine for instant display)
		string casterName = EventLogPanel.GetColoredDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetColoredDisplayNameForUnit(target) : "self";
		EventLogPanel.LogEvent($"{casterName} uses {skill.skillName} on {targetName}!");
		
		// Start coroutine for delayed effects
		StartCoroutine(ExecuteSkillWithDelayCoroutine(caster, skill, target, autoAdvanceTurn));
	}
	
	/// <summary>
	/// Public method to execute a skill directly (not from unit's skills array) with proper timing delays
	/// </summary>
	public void ExecuteSkillDirect(Unit caster, Skill skill, Unit target, bool autoAdvanceTurn = false)
	{
		if (skill == null || caster == null)
		{
			Debug.LogWarning($"Invalid skill or caster for ExecuteSkillDirect");
			return;
		}
		
		// Hide action UI immediately to prevent input during skill execution
		HideActionUI();
		
		// Log skill usage immediately (before starting coroutine for instant display)
		string casterName = EventLogPanel.GetColoredDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetColoredDisplayNameForUnit(target) : "self";
		EventLogPanel.LogEvent($"{casterName} uses {skill.skillName} on {targetName}!");
		
		// Start coroutine for delayed effects
		StartCoroutine(ExecuteSkillWithDelayCoroutine(caster, skill, target, autoAdvanceTurn));
	}
	
	/// <summary>
	/// Coroutine to execute enemy skill with proper timing delays
	/// </summary>
	private System.Collections.IEnumerator ExecuteEnemySkillWithDelay(Unit caster, int skillIndex, Unit target)
	{
		if (skillIndex < 0 || skillIndex >= caster.Skills.Length)
		{
			Debug.LogWarning($"Invalid skill index {skillIndex} for enemy unit {caster.UnitName}");
			ShowActionUI(); // Restore UI in case of error
			yield break;
		}
			
		Skill skill = caster.Skills[skillIndex];
		
		// Set cooldown (same as UseSkill does)
		caster.SetSkillCooldown(skillIndex, skill.cooldownTurns);
		
		// Log skill usage immediately
		string casterName = EventLogPanel.GetColoredDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetColoredDisplayNameForUnit(target) : "self";
		EventLogPanel.LogEvent($"{casterName} uses {skill.skillName} on {targetName}!");
		
		// Use shared coroutine for skill execution
		yield return StartCoroutine(ExecuteSkillWithDelayCoroutine(caster, skill, target, autoAdvanceTurn: true));
	}
	
	/// <summary>
	/// Coroutine to execute skill with proper timing delays (shared by both player and enemy paths)
	/// </summary>
	private System.Collections.IEnumerator ExecuteSkillWithDelayCoroutine(Unit caster, Skill skill, Unit target, bool autoAdvanceTurn)
	{
		if (skill == null || caster == null)
		{
			Debug.LogError("ExecuteSkillWithDelayCoroutine: Invalid caster or skill!");
			ShowActionUI(); // Restore UI in case of error
			yield break;
		}
		
		// Wait for skill animation
		yield return new WaitForSeconds(skillAnimationDelay);
		
		// Check if caster is still alive before applying skill effects
		if (caster == null || !caster.IsAlive())
		{
			Debug.LogWarning($"Caster {caster?.gameObject.name} died during skill animation delay! Advancing turn.");
			ShowActionUI();
			if (autoAdvanceTurn)
			{
				AdvanceToNextTurn();
			}
			yield break;
		}
		
		// Apply skill effects (which will log damage and start attack animation)
		caster.ApplySkillEffects(skill, target);
		
		// Wait for attack-to-hurt delay (damage is applied during this time)
		// If using animation-based delay, wait for animation + post delay
		float delayToWait = GetAttackToHurtDelay(caster);
		yield return new WaitForSeconds(delayToWait);
		
		// Wait for hit animation
		yield return new WaitForSeconds(hitAnimationDelay);
		
		// Show action UI again
		ShowActionUI();
		
		// Advance turn if auto-advance is enabled (for enemy turns)
		if (autoAdvanceTurn)
		{
			AdvanceToNextTurn();
		}
	}
	
	/// <summary>
	/// Gets the appropriate attack-to-hurt delay based on caster's animation length
	/// </summary>
	private float GetAttackToHurtDelay(Unit caster)
	{
		if (caster != null)
		{
			UnitAnimations unitAnimations = caster.GetComponent<UnitAnimations>();
			if (unitAnimations != null)
			{
				float animationLength = unitAnimations.GetAttackAnimationLength();
				if (animationLength > 0f)
				{
					// Return animation length + post-animation delay
					return animationLength + postAttackAnimationDelay;
				}
			}
		}
		// Fallback if animation length can't be determined
		return 0.5f;
	}
	
	#endregion
    
    #region UI Management
    
    /// <summary>
    /// Notifies SkillPanelManager to update when unit changes
    /// </summary>
	private void UpdateSkillPanel()
	{
		SkillPanelManager skillPanel = FindFirstObjectByType<SkillPanelManager>();
		if (skillPanel != null)
		{
			skillPanel.UpdateSkills();
		}
	}
	
	/// <summary>
	/// Hides action UI elements to prevent player input during skill execution
	/// </summary>
	private void HideActionUI()
	{
		// Hide user panel root which contains all UI elements
		HideUserPanel();
		
		// Also notify ActionPanelManager to set the skill executing flag
		if (actionPanelManager == null)
		{
			actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
		}
		
		if (actionPanelManager != null)
		{
			actionPanelManager.HideAllActionUI();
		}
	}
	
	/// <summary>
	/// Shows action UI elements after skill execution completes
	/// </summary>
	private void ShowActionUI()
	{
		// Show user panel root
		ShowUserPanel();
		
		// Show ActionPanelManager elements (they will handle visibility based on current unit)
		if (actionPanelManager == null)
		{
			actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
		}
		
		if (actionPanelManager != null)
		{
			actionPanelManager.ShowAllActionUI();
		}
	}
	
	/// <summary>
	/// Hides the user panel root (contains action panel, skill panel, item panel, unit name, end turn button)
	/// </summary>
	public void HideUserPanel()
	{
		if (userPanelRoot != null)
		{
			userPanelRoot.SetActive(false);
		}
	}
	
	/// <summary>
	/// Shows the user panel root (contains action panel, skill panel, item panel, unit name, end turn button)
	/// </summary>
	public void ShowUserPanel()
	{
		if (userPanelRoot != null)
		{
			userPanelRoot.SetActive(true);
		}
	}
	
	/// <summary>
	/// Hides all UI elements: creature UI roots, enemy UI roots, user panel, turn order timeline, and event log
	/// </summary>
	public void HideAllUI()
	{
		// Ensure this GameManager GameObject stays active
		if (!gameObject.activeSelf)
		{
			Debug.LogWarning("GameManager: GameManager GameObject was inactive! Activating it now.");
			gameObject.SetActive(true);
		}
		
		// Helper function to check if hiding a GameObject would affect GameManager
		System.Func<GameObject, bool> canSafelyHide = (go) =>
		{
			if (go == null || go == gameObject)
				return false;
			
			// Check if GameManager is a child of this GameObject (if so, hiding it would hide GameManager)
			if (gameObject.transform.IsChildOf(go.transform))
			{
				Debug.LogWarning($"GameManager: Cannot hide {go.name} - GameManager is a child of it!");
				return false;
			}
			
			// Check if this GameObject is a child of GameManager (safe to hide children)
			if (go.transform.IsChildOf(gameObject.transform))
				return true;
			
			// If they're not related, it's safe to hide
			return true;
		};
		
		// Hide gameplay UI parent (contains statusDisplayPanel, turnOrderTimeline, eventPanel, infoPanel, playerInfo)
		if (gameplayUI != null)
		{
			if (canSafelyHide(gameplayUI))
			{
				gameplayUI.SetActive(false);
				Debug.Log($"GameManager: Hid gameplayUI: {gameplayUI.name}");
			}
			else
			{
				Debug.LogWarning($"GameManager: Cannot hide gameplayUI: {gameplayUI.name} - safety check failed");
			}
		}
		
		// Hide user panel (this will hide ActionPanel and other child panels)
		// Keep separate because it needs to be shown/hidden during turns
		if (userPanelRoot != null)
		{
			if (canSafelyHide(userPanelRoot))
			{
				userPanelRoot.SetActive(false);
				Debug.Log($"GameManager: Hid userPanelRoot: {userPanelRoot.name}");
			}
			else
			{
				Debug.LogWarning($"GameManager: Cannot hide userPanelRoot: {userPanelRoot.name} - safety check failed");
			}
		}
		
		// Hide creature spawn area
		if (creatureSpawnArea != null && creatureSpawnArea.gameObject != null)
		{
			if (canSafelyHide(creatureSpawnArea.gameObject))
			{
				creatureSpawnArea.gameObject.SetActive(false);
				Debug.Log($"GameManager: Hid creatureSpawnArea: {creatureSpawnArea.name}");
			}
			else
			{
				Debug.LogWarning($"GameManager: Cannot hide creatureSpawnArea: {creatureSpawnArea.name} - safety check failed");
			}
		}
		
		// Hide enemy spawn area
		if (enemySpawnArea != null && enemySpawnArea.gameObject != null)
		{
			if (canSafelyHide(enemySpawnArea.gameObject))
			{
				enemySpawnArea.gameObject.SetActive(false);
				Debug.Log($"GameManager: Hid enemySpawnArea: {enemySpawnArea.name}");
			}
			else
			{
				Debug.LogWarning($"GameManager: Cannot hide enemySpawnArea: {enemySpawnArea.name} - safety check failed");
			}
		}
		
		// Hide settings button (keep separate if you want it to persist, or move it into gameplayUI if it should hide)
		if (settingsButton != null)
		{
			if (canSafelyHide(settingsButton))
			{
				settingsButton.SetActive(false);
				Debug.Log($"GameManager: Hid settingsButton: {settingsButton.name}");
			}
			else
			{
				Debug.LogWarning($"GameManager: Cannot hide settingsButton: {settingsButton.name} - safety check failed");
			}
		}
		
		// Final check - ensure GameManager is still active after hiding UI
		if (!gameObject.activeSelf)
		{
			Debug.LogError("GameManager: GameManager GameObject became inactive after HideAllUI()! This should not happen.");
			gameObject.SetActive(true);
		}
	}
	
	/// <summary>
	/// Shows all UI elements that were hidden at start: status display panel, user panel, turn order timeline, event panel, info panel, current level text, current gold text, settings button, and spawn areas
	/// </summary>
	/// <param name="showTurnOrderTimeline">Whether to show the turn order timeline immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showStatusDisplayPanel">Whether to show the status display panel immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showEventPanel">Whether to show the event panel immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showCurrentLevelText">Whether to show the current level text immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showCurrentGoldText">Whether to show the current gold text immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showSettingsButton">Whether to show the settings button immediately (set to false to delay showing until after initialization)</param>
	/// <param name="showSpawnAreas">Whether to show the spawn areas immediately (set to false to delay showing until after initialization)</param>
	public void ShowAllUI(bool showTurnOrderTimeline = true, bool showStatusDisplayPanel = true, bool showEventPanel = true, bool showCurrentLevelText = true, bool showCurrentGoldText = true, bool showSettingsButton = true, bool showSpawnAreas = true)
	{
		// Show gameplay UI parent (contains statusDisplayPanel, turnOrderTimeline, eventPanel, infoPanel, playerInfo)
		// Show if any of the individual components are requested
		if ((showStatusDisplayPanel || showTurnOrderTimeline || showEventPanel || showCurrentLevelText || showCurrentGoldText) && gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI: {gameplayUI.name}");
		}
		
		// Show user panel
		if (userPanelRoot != null)
		{
			userPanelRoot.SetActive(true);
			Debug.Log($"GameManager: Showed userPanelRoot: {userPanelRoot.name}");
		}
		
		// Show settings button (only if requested - can be delayed until after initialization)
		if (showSettingsButton && settingsButton != null)
		{
			settingsButton.SetActive(true);
			Debug.Log($"GameManager: Showed settingsButton: {settingsButton.name}");
		}
		
		// Show creature spawn area (only if requested - can be delayed until after initialization)
		if (showSpawnAreas && creatureSpawnArea != null && creatureSpawnArea.gameObject != null)
		{
			creatureSpawnArea.gameObject.SetActive(true);
			Debug.Log($"GameManager: Showed creatureSpawnArea: {creatureSpawnArea.name}");
		}
		
		// Show enemy spawn area (only if requested - can be delayed until after initialization)
		if (showSpawnAreas && enemySpawnArea != null && enemySpawnArea.gameObject != null)
		{
			enemySpawnArea.gameObject.SetActive(true);
			Debug.Log($"GameManager: Showed enemySpawnArea: {enemySpawnArea.name}");
		}
	}
	
	/// <summary>
	/// Shows the turn order timeline (used after game initialization to prevent showing incorrect information)
	/// Shows the gameplayUI parent which contains this element
	/// </summary>
	public void ShowTurnOrderTimeline()
	{
		if (gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI (turnOrderTimeline): {gameplayUI.name}");
		}
	}
	
	/// <summary>
	/// Shows the status display panel (used after game initialization to prevent showing incorrect information)
	/// Shows the gameplayUI parent which contains this element
	/// </summary>
	public void ShowStatusDisplayPanel()
	{
		if (gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI (statusDisplayPanel): {gameplayUI.name}");
		}
	}
	
	/// <summary>
	/// Shows the event panel (used after game initialization to prevent showing incorrect information)
	/// Shows the gameplayUI parent which contains this element
	/// </summary>
	public void ShowEventPanel()
	{
		if (gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI (eventPanel): {gameplayUI.name}");
		}
	}
	
	/// <summary>
	/// Shows the current level text (used after game initialization to prevent showing incorrect information)
	/// </summary>
	public void ShowCurrentLevelText()
	{
		if (playerInfo != null)
		{
			playerInfo.SetActive(true);
			Debug.Log($"GameManager: Showed playerInfo (currentLevelText): {playerInfo.name}");
		}
	}
	
	/// <summary>
	/// Shows the current gold text (used after game initialization to prevent showing incorrect information)
	/// </summary>
	public void ShowCurrentGoldText()
	{
		if (playerInfo != null)
		{
			playerInfo.SetActive(true);
			Debug.Log($"GameManager: Showed playerInfo (currentGoldText): {playerInfo.name}");
		}
	}
	
	/// <summary>
	/// Shows the player info panel (contains both currentLevelText and currentGoldText)
	/// Shows the gameplayUI parent which contains this element
	/// </summary>
	public void ShowPlayerInfo()
	{
		if (gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI (playerInfo): {gameplayUI.name}");
		}
	}
	
	/// <summary>
	/// Shows the gameplay UI parent (contains statusDisplayPanel, turnOrderTimeline, eventPanel, infoPanel, playerInfo)
	/// </summary>
	public void ShowGameplayUI()
	{
		if (gameplayUI != null)
		{
			gameplayUI.SetActive(true);
			Debug.Log($"GameManager: Showed gameplayUI: {gameplayUI.name}");
		}
	}
	
	/// <summary>
	/// Shows the settings button (used after game initialization to prevent showing incorrect information)
	/// </summary>
	public void ShowSettingsButton()
	{
		if (settingsButton != null)
		{
			settingsButton.SetActive(true);
			Debug.Log($"GameManager: Showed settingsButton: {settingsButton.name}");
		}
	}
	
	/// <summary>
	/// Shows the spawn areas (used after game initialization to prevent units from appearing before UI is ready)
	/// </summary>
	public void ShowSpawnAreas()
	{
		if (creatureSpawnArea != null && creatureSpawnArea.gameObject != null)
		{
			creatureSpawnArea.gameObject.SetActive(true);
			Debug.Log($"GameManager: Showed creatureSpawnArea: {creatureSpawnArea.name}");
		}
		
		if (enemySpawnArea != null && enemySpawnArea.gameObject != null)
		{
			enemySpawnArea.gameObject.SetActive(true);
			Debug.Log($"GameManager: Showed enemySpawnArea: {enemySpawnArea.name}");
		}
	}
	
	#endregion
    
    #region Unit UI Management
    
    /// <summary>
    /// Updates all unit UI displays by connecting units to their corresponding UI elements based on spawn index
    /// </summary>
	public void UpdateAllUnitUI()
	{
		if (turnOrderRef == null)
		{
			turnOrderRef = FindFirstObjectByType<TurnOrder>();
		}
		
		// Find all units in the scene
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		
		// Clear all UI first
		ClearCreatureUI();
		ClearEnemyUI();
		
        // Connect each unit to its UI based on which spawn area array it belongs to
		int aliveAlliesCount = 0;
		int aliveEnemiesCount = 0;
		
		foreach (var unit in allUnits)
		{
			if (unit == null || !unit.IsAlive()) continue;
			
			// Check if unit is in creature spawn areas or enemy spawn areas
			int creatureIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: true);
			if (creatureIndex >= 0)
			{
				aliveAlliesCount++;
				ConnectUnitToCreatureUI(unit, creatureIndex);
				continue;
			}
			
			int enemyIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: false);
			if (enemyIndex >= 0)
			{
				aliveEnemiesCount++;
				ConnectUnitToEnemyUI(unit, enemyIndex);
			}
		}
		
		// Hide unused UI roots based on alive unit counts
		HideUnusedCreatureUIRoots(aliveAlliesCount);
		HideUnusedEnemyUIRoots(aliveEnemiesCount);
	}
	
	/// <summary>
	/// Connects a unit to its creature UI slot
	/// </summary>
	private void ConnectUnitToCreatureUI(Unit unit, int creatureIndex)
	{
		// Creature UI (indices 0-2)
		if (creatureNameTexts[creatureIndex] != null)
		{
			string displayName = EventLogPanel.GetDisplayNameForUnit(unit);
			creatureNameTexts[creatureIndex].text = displayName;
			creatureNameTexts[creatureIndex].fontSize = CalculateFontSize(displayName);
			creatureNameTexts[creatureIndex].color = allyColor;
			creatureNameTexts[creatureIndex].gameObject.SetActive(true);
		}
		
		if (creatureHealthFills[creatureIndex] != null)
		{
			creatureHealthFills[creatureIndex].gameObject.SetActive(true);
			UpdateUnitHealthUI(unit, creatureIndex, isCreature: true);
			unit.SetHealthFill(creatureHealthFills[creatureIndex]);
		}
		
		if (creatureActionGaugeFills[creatureIndex] != null)
		{
			creatureActionGaugeFills[creatureIndex].gameObject.SetActive(true);
			UpdateUnitActionGaugeUI(unit, creatureIndex, isCreature: true);
		}
        
        // Ensure the creature UI root is active for alive unit
        if (creatureUIRoots != null && creatureIndex >= 0 && creatureIndex < creatureUIRoots.Length && creatureUIRoots[creatureIndex] != null)
        {
            creatureUIRoots[creatureIndex].SetActive(true);
        }
	}
	
	/// <summary>
	/// Connects a unit to its enemy UI slot
	/// </summary>
	private void ConnectUnitToEnemyUI(Unit unit, int enemyIndex)
	{
		// Enemy UI (indices 0-2)
		if (enemyNameTexts[enemyIndex] != null)
		{
			string displayName = EventLogPanel.GetDisplayNameForUnit(unit);
			enemyNameTexts[enemyIndex].text = displayName;
			enemyNameTexts[enemyIndex].fontSize = CalculateFontSize(displayName);
			enemyNameTexts[enemyIndex].color = enemyColor;
			enemyNameTexts[enemyIndex].gameObject.SetActive(true);
		}
		
		if (enemyHealthFills[enemyIndex] != null)
		{
			enemyHealthFills[enemyIndex].gameObject.SetActive(true);
			UpdateUnitHealthUI(unit, enemyIndex, isCreature: false);
			unit.SetHealthFill(enemyHealthFills[enemyIndex]);
		}
		
		if (enemyActionGaugeFills[enemyIndex] != null)
		{
			enemyActionGaugeFills[enemyIndex].gameObject.SetActive(true);
			UpdateUnitActionGaugeUI(unit, enemyIndex, isCreature: false);
		}
        
        // Ensure the enemy UI root is active for alive unit
        if (enemyUIRoots != null && enemyIndex >= 0 && enemyIndex < enemyUIRoots.Length && enemyUIRoots[enemyIndex] != null)
        {
            enemyUIRoots[enemyIndex].SetActive(true);
        }
	}
	
	/// <summary>
	/// Hides unused creature UI roots based on alive unit count
	/// </summary>
	private void HideUnusedCreatureUIRoots(int aliveAlliesCount)
	{
		// Hide the 3rd UI root (index 2) if there are fewer than 3 units on the field
		if (aliveAlliesCount < 3 && creatureUIRoots != null && creatureUIRoots.Length > 2 && creatureUIRoots[2] != null)
		{
			creatureUIRoots[2].SetActive(false);
		}
		
		// Hide the 2nd UI root (index 1) if there are fewer than 2 units on the field
		if (aliveAlliesCount < 2 && creatureUIRoots != null && creatureUIRoots.Length > 1 && creatureUIRoots[1] != null)
		{
			creatureUIRoots[1].SetActive(false);
		}
	}
	
	/// <summary>
	/// Hides unused enemy UI roots based on alive unit count
	/// </summary>
	private void HideUnusedEnemyUIRoots(int aliveEnemiesCount)
	{
		if (aliveEnemiesCount < 3 && enemyUIRoots != null && enemyUIRoots.Length > 2 && enemyUIRoots[2] != null)
		{
			enemyUIRoots[2].SetActive(false);
		}
		
		// Hide the 2nd UI root (index 1) if there are fewer than 2 units on the field
		if (aliveEnemiesCount < 2 && enemyUIRoots != null && enemyUIRoots.Length > 1 && enemyUIRoots[1] != null)
		{
			enemyUIRoots[1].SetActive(false);
		}
	}
	
	/// <summary>
	/// Clears all creature UI displays
	/// </summary>
	private void ClearCreatureUI()
	{
		for (int i = 0; i < 3; i++)
		{
			if (creatureNameTexts[i] != null)
			{
				creatureNameTexts[i].text = "";
				creatureNameTexts[i].gameObject.SetActive(false);
			}
			if (creatureHealthFills[i] != null)
			{
				creatureHealthFills[i].fillAmount = 0f;
				creatureHealthFills[i].gameObject.SetActive(false);
			}
			if (creatureActionGaugeFills[i] != null)
			{
				creatureActionGaugeFills[i].fillAmount = 0f;
				creatureActionGaugeFills[i].gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// Clears all enemy UI displays
	/// </summary>
	private void ClearEnemyUI()
	{
		for (int i = 0; i < 3; i++)
		{
			if (enemyNameTexts[i] != null)
			{
				enemyNameTexts[i].text = "";
				enemyNameTexts[i].gameObject.SetActive(false);
			}
			if (enemyHealthFills[i] != null)
			{
				enemyHealthFills[i].fillAmount = 0f;
				enemyHealthFills[i].gameObject.SetActive(false);
			}
			if (enemyActionGaugeFills[i] != null)
			{
				enemyActionGaugeFills[i].fillAmount = 0f;
				enemyActionGaugeFills[i].gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// Updates a specific unit's health UI
	/// </summary>
	private void UpdateUnitHealthUI(Unit unit, int spawnIndex, bool isCreature)
	{
		if (unit == null || spawnIndex < 0 || spawnIndex >= 3) return;
		
		UnityEngine.UI.Image healthFill = null;
		
		if (isCreature && creatureHealthFills[spawnIndex] != null)
		{
			healthFill = creatureHealthFills[spawnIndex];
		}
		else if (!isCreature && enemyHealthFills[spawnIndex] != null)
		{
			healthFill = enemyHealthFills[spawnIndex];
		}
		
		if (healthFill != null)
		{
			float healthPercentage = unit.GetHPPercentage();
			healthFill.fillAmount = healthPercentage;
		}
	}
	
	/// <summary>
	/// Updates a specific unit's action gauge UI
	/// </summary>
	private void UpdateUnitActionGaugeUI(Unit unit, int spawnIndex, bool isCreature)
	{
		if (unit == null || spawnIndex < 0 || spawnIndex >= 3) return;
		
		UnityEngine.UI.Image actionGaugeFill = null;
		
		if (isCreature && creatureActionGaugeFills[spawnIndex] != null)
		{
			actionGaugeFill = creatureActionGaugeFills[spawnIndex];
		}
		else if (!isCreature && enemyActionGaugeFills[spawnIndex] != null)
		{
			actionGaugeFill = enemyActionGaugeFills[spawnIndex];
		}
		
		if (actionGaugeFill != null)
		{
			// Action gauge is 0-100, so divide by 100 to get percentage
			float gaugePercentage = Mathf.Clamp01(unit.GetActionGauge() / 100f);
			actionGaugeFill.fillAmount = gaugePercentage;
		}
	}
	
	/// <summary>
	/// Calculates the font size based on the length of the unit name
	/// Shorter names use max font size, longer names scale down to min font size
	/// </summary>
	private float CalculateFontSize(string name)
	{
		if (string.IsNullOrEmpty(name))
			return maxFontSize;
		
		int nameLength = name.Length;
		
		// If name is at or below short threshold, use max font size
		if (nameLength <= shortNameThreshold)
		{
			return maxFontSize;
		}
		
		// If name is at or above long threshold, use min font size
		if (nameLength >= longNameThreshold)
		{
			return minFontSize;
		}
		
		// Interpolate between max and min based on name length
		// Calculate how far along the range we are (0 = at short threshold, 1 = at long threshold)
		float range = longNameThreshold - shortNameThreshold;
		float position = (nameLength - shortNameThreshold) / range;
		
		// Lerp from max to min
		return Mathf.Lerp(maxFontSize, minFontSize, position);
	}
	
	/// <summary>
	/// Updates all units' action gauge UI displays
	/// </summary>
	private void UpdateAllActionGaugeUI()
	{
		if (turnOrderRef == null)
		{
			turnOrderRef = FindFirstObjectByType<TurnOrder>();
		}
		
		// Find all units in the scene
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		
		foreach (var unit in allUnits)
		{
			if (unit == null || !unit.IsAlive()) continue;
			
			// Check if unit is in creature spawn areas or enemy spawn areas
			int creatureIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: true);
			if (creatureIndex >= 0)
			{
				UpdateUnitActionGaugeUI(unit, creatureIndex, isCreature: true);
				continue;
			}
			
			int enemyIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: false);
			if (enemyIndex >= 0)
			{
				UpdateUnitActionGaugeUI(unit, enemyIndex, isCreature: false);
			}
		}
	}
	
	/// <summary>
	/// Called when a unit's health changes - updates the corresponding UI
	/// </summary>
	public void OnUnitHealthChanged(Unit unit)
	{
		if (unit == null) return;
		
		int spawnIndex = GetUnitSpawnIndexWithinType(unit);
		if (spawnIndex >= 0 && spawnIndex < 3)
		{
			UpdateUnitHealthUI(unit, spawnIndex, isCreature: unit.IsPlayerUnit);

            // Toggle the corresponding UI root based on alive state
            bool isAlive = unit.IsAlive();
            if (unit.IsPlayerUnit)
            {
                if (creatureUIRoots != null && spawnIndex < creatureUIRoots.Length && creatureUIRoots[spawnIndex] != null)
                {
                    creatureUIRoots[spawnIndex].SetActive(isAlive);
                }
            }
            else
            {
                if (enemyUIRoots != null && spawnIndex < enemyUIRoots.Length && enemyUIRoots[spawnIndex] != null)
                {
                    enemyUIRoots[spawnIndex].SetActive(isAlive);
                }
            }
		}
	}
	
	#endregion
    
    #region Spawn Index Lookup
    
    /// <summary>
    /// Gets the spawn area index (0-2) within the creature or enemy array for a unit
    /// Returns -1 if not found in the specified array
    /// </summary>
	private int GetUnitSpawnAreaIndexInArray(Unit unit, bool isCreature)
	{
		if (unit == null) return -1;
		
		// Get reference to Spawning to access spawn areas
		Spawning spawning = FindFirstObjectByType<Spawning>();
		if (spawning == null) return -1;
		
		// Get the appropriate spawn areas array
		string fieldName = isCreature ? "creatureSpawnAreas" : "enemySpawnAreas";
		var spawnAreasField = typeof(Spawning).GetField(fieldName, 
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		
		if (spawnAreasField != null)
		{
			var spawnAreas = spawnAreasField.GetValue(spawning) as Transform[];
			if (spawnAreas != null)
			{
				// Find which spawn area the unit belongs to by checking parent hierarchy
				for (int i = 0; i < spawnAreas.Length; i++)
				{
					if (spawnAreas[i] != null)
					{
						// Check if unit is a child of this spawn area
						Transform unitTransform = unit.transform;
						while (unitTransform != null)
						{
							if (unitTransform == spawnAreas[i])
							{
								return i; // Return index within the array (0-2)
							}
							unitTransform = unitTransform.parent;
						}
					}
				}
			}
		}
		
		return -1;
	}
	
	/// <summary>
	/// Gets the UI index (0-2) within the unit's team type (creature or enemy) based on spawn area
	/// Returns -1 if not found
	/// </summary>
	private int GetUnitSpawnIndexWithinType(Unit unit)
	{
		// Try creature array first
		int creatureIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: true);
		if (creatureIndex >= 0)
		{
			return creatureIndex;
		}
		
		// Try enemy array
		int enemyIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: false);
		if (enemyIndex >= 0)
		{
			return enemyIndex;
		}
		
		return -1;
	}
	
	#endregion
    
    #region Round End Management
    
    /// <summary>
    /// Called when all allies/player units are dead - opens the round end panel with "Game Over!" message
    /// </summary>
	public void OnAllAlliesDead()
	{
		// Delay showing the game over screen to allow death animations to play
		StartCoroutine(DelayedShowRoundEndPanel("Game Over!"));
	}
	
	/// <summary>
	/// Called when all enemies are dead - opens the map panel instead of round end panel
	/// If boss is defeated (stage 3), shows round end panel with "Zone cleared" instead
	/// </summary>
	public void OnAllEnemiesDead()
	{
        // Hide action UI during round end
        if (actionPanelManager == null)
        {
            actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        }
        if (actionPanelManager != null)
        {
            actionPanelManager.HideForRoundEnd();
        }

		// Check if the last completed node was a Boss node type
		bool isBossDefeated = false;
		Map.MapManager mapManager = FindFirstObjectByType<Map.MapManager>();
		if (mapManager != null && mapManager.CurrentMap != null && mapManager.CurrentMap.path.Count > 0)
		{
			// Get the last node in the path (the one that was just completed)
			Vector2Int lastNodePoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
			Map.Node lastNode = mapManager.CurrentMap.GetNode(lastNodePoint);
			
			if (lastNode != null && lastNode.nodeType == Map.NodeType.Boss)
			{
				isBossDefeated = true;
			}
		}

		// Award gold for winning the round (every round win, not just boss defeats)
		AwardFloorCompletionGold();
		
		if (isBossDefeated)
		{
			// Boss defeated - show round end panel with "Zone cleared" instead of map panel
			StartCoroutine(DelayedShowRoundEndPanel("Zone cleared"));
		}
		else
		{
			// Normal level - delay showing the map panel to allow death animations to play
			StartCoroutine(DelayedShowMapPanel());
		}
	}
	
	/// <summary>
	/// Coroutine to delay showing the round end panel to allow death animations to play
	/// </summary>
	private System.Collections.IEnumerator DelayedShowRoundEndPanel(string message)
	{
		// Wait for post-attack animation delay + additional round end screen delay
		float totalDelay = postAttackAnimationDelay + roundEndScreenDelay;
		yield return new WaitForSeconds(totalDelay);
		ShowRoundEndPanel(message);
	}
	
	/// <summary>
	/// Coroutine to delay showing the map panel to allow death animations to play
	/// </summary>
	private System.Collections.IEnumerator DelayedShowMapPanel()
	{
		// Wait for post-attack animation delay + additional round end screen delay
		float totalDelay = postAttackAnimationDelay + roundEndScreenDelay;
		yield return new WaitForSeconds(totalDelay);
		
		// Find LevelMap and show the map panel
		LevelMap levelMap = FindFirstObjectByType<LevelMap>();
		if (levelMap != null)
		{
			levelMap.ShowMapPanel();
		}
		else
		{
			Debug.LogWarning("GameManager: LevelMap not found! Falling back to round end panel.");
			// Fallback to old behavior if LevelMap not found
			string message = "Round Won!";
			LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
			if (levelNavigation != null && levelNavigation.GetCurrentLevel() == "B1-3")
			{
				message = "Game Won!";
			}
			ShowRoundEndPanel(message);
		}
	}
	
	/// <summary>
	/// Shows the round end panel with the specified message
	/// </summary>
	private void ShowRoundEndPanel(string message)
	{
		if (roundEndPanel != null)
		{
			// Hide UserPanel and GameplayUI when "Zone cleared" message is shown
			if (message == "Zone cleared")
			{
				HideUserPanel();
				if (gameplayUI != null)
				{
					gameplayUI.SetActive(false);
					Debug.Log($"GameManager: Hid gameplayUI for Zone cleared message");
				}
			}
			
			// Update the message text
			if (roundEndMessageText != null)
			{
				roundEndMessageText.text = message;
			}
			else
			{
				Debug.LogWarning("GameManager: roundEndMessageText is not assigned!");
			}
			
			// Show/hide next level button based on the message
			if (nextLevelButton != null)
			{
				// Hide button on "Game Over!", "Game Won!", or "Zone cleared"
				// Show it only on "Round Won!"
				bool shouldShowButton = (message == "Round Won!");
				nextLevelButton.gameObject.SetActive(shouldShowButton);
				
				// Also disable the button interactivity for "Zone cleared" (in case it's shown but should be disabled)
				if (message == "Zone cleared")
				{
					nextLevelButton.interactable = false;
				}
				else if (shouldShowButton)
				{
					nextLevelButton.interactable = true;
				}
			}
			
			// Show the panel
			roundEndPanel.SetActive(true);
		}
		else
		{
			Debug.LogWarning("GameManager: roundEndPanel is not assigned!");
		}
	}
	
	#endregion
    
    #region Button Handlers
    
    /// <summary>
    /// Called when the return to main menu button is clicked
    /// </summary>
	public void OnReturnToMainMenuClicked()
	{
		LoadMainMenuScene();
	}
	
	/// <summary>
	/// Called when the restart round button is clicked - reloads the active scene
	/// </summary>
	public void OnRestartRoundClicked()
	{
		Scene current = SceneManager.GetActiveScene();
		SceneManager.LoadScene(current.buildIndex);
	}
	
	/// <summary>
	/// Called when the next level button is clicked - advances to the next level
	/// </summary>
	public void OnNextLevelClicked()
	{
		LevelNavigation levelNavigation = FindFirstObjectByType<LevelNavigation>();
		if (levelNavigation != null)
		{
			levelNavigation.AdvanceToNextLevel();
		}
		else
		{
			Debug.LogWarning("GameManager: LevelNavigation not found! Cannot advance to next level.");
		}
	}
	
	/// <summary>
	/// Loads the main menu scene
	/// </summary>
	private void LoadMainMenuScene()
	{
		if (!string.IsNullOrEmpty(mainMenuSceneName))
		{
			SceneManager.LoadScene(mainMenuSceneName);
		}
		else
		{
			Debug.LogWarning("GameManager: mainMenuSceneName is not set! Cannot load main menu.");
		}
	}
	
	#endregion
    
    #region Debug Utilities
    
    /// <summary>
    /// Debug utility: Prints the action gauge status of all units
    /// Press G key to use this
    /// </summary>
	private void PrintActionGaugeStatus()
	{
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		
		Debug.Log("=== ACTION GAUGE STATUS ===");
		
		if (allUnits == null || allUnits.Length == 0)
		{
			Debug.Log("No units found in scene.");
			return;
		}
		
		foreach (var unit in allUnits)
		{
			if (unit == null) continue;
			
			string team = unit.IsPlayerUnit ? "PLAYER" : "ENEMY";
			string alive = unit.IsAlive() ? "ALIVE" : "DEAD";
			float gauge = unit.GetActionGauge();
			bool canAct = gauge >= 100f;
			string canActStr = canAct ? "✓ CAN ACT" : "✗ Cannot act";
			
			Debug.Log($"[{team}] {unit.gameObject.name}: Speed={unit.Speed}, Gauge={gauge:F1}/100 ({gauge/100f*100:F1}%), {alive}, {canActStr}");
		}
		
		Unit current = GetCurrentUnit();
		if (current != null)
		{
			Debug.Log($"\nCURRENT UNIT: {current.gameObject.name} (Speed: {current.Speed}, Gauge: {current.GetActionGauge():F1})");
		}
		else
		{
			Debug.Log("\nCURRENT UNIT: None");
		}
		
		Debug.Log("============================");
	}
	
	#endregion
}
