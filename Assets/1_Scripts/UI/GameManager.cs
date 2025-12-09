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
    
    [Header("Unit UI Roots (toggle on death)")]
    [Tooltip("Root GameObjects for creature UI slots (0-2). These will be disabled when the unit dies.")]
    public GameObject[] creatureUIRoots = new GameObject[3];
    
    [Tooltip("Root GameObjects for enemy UI slots (0-2). These will be disabled when the unit dies.")]
    public GameObject[] enemyUIRoots = new GameObject[3];

	 [Header("User Panel UI Root")]
    public GameObject userPanelRoot;
	
	[Header("Game Over Panels")]
	[Tooltip("Panel that opens when all allies/player units are dead")]
	public GameObject allAlliesDeadPanel;
	
	[Tooltip("Button on the game over panel that returns to main menu")]
	public Button returnToMainMenuButton;
	
	[Tooltip("Name of the main menu scene to load")]
	public string mainMenuSceneName = "MainMenu";
	
    private TurnOrder turnOrderRef; // Reference to get spawn indices

    [Header("Turn Management")]
    private Unit currentUnit;
    
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
    
    [Tooltip("Delay in seconds for hit animation to play (between damage message and turn advancement)")]
    [Range(0f, 5f)]
    public float hitAnimationDelay = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find TurnOrder if not assigned
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
        }
        
        // Cache ActionPanelManager reference
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        
        // Set up return to main menu button listener
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuClicked);
        }
        
        // Hide game over panel at start
        if (allAlliesDeadPanel != null)
        {
            allAlliesDeadPanel.SetActive(false);
        }

        // Get the first unit that should go - delay slightly to ensure all units are initialized
        StartCoroutine(DelayedStart());
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        // Wait a frame to ensure all units are fully initialized
        yield return null;
        
        // Get reference to TurnOrder for spawn index lookup
        if (turnOrder != null)
        {
            turnOrderRef = turnOrder;
        }
        else
        {
            turnOrderRef = FindFirstObjectByType<TurnOrder>();
        }
        
        // Connect all units to their UI elements
        UpdateAllUnitUI();
        
        if (turnOrder != null)
        {
            // Increment all units' gauges until someone reaches 100 for the first turn
            // This makes the first turn selection based on gauge accumulation, not just initial speed
            Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (allUnits != null && allUnits.Length > 0)
            {
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
                Debug.LogWarning("GameManager: No first unit found! All units may have gauge < 100.");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: No TurnOrder component found in scene!");
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
			if (turnOrder != null)
			{
				turnOrder.AdvanceToNextTurn();
			}
			return;
		}
		
		// Get all alive player units (targetable by enemies)
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
		
		if (playerUnits.Count == 0)
		{
			Debug.LogWarning($"No alive player units to target! Total units found: {allUnits.Length}. Advancing turn.");
			Debug.LogWarning("Checking all units:");
			foreach (var unit in allUnits)
			{
				if (unit != null)
				{
					Debug.LogWarning($"  - {unit.gameObject.name}: IsPlayerUnit={unit.IsPlayerUnit}, IsAlive={unit.IsAlive()}");
				}
			}
			// Advance turn immediately since there are no valid targets
			if (turnOrder != null)
			{
				turnOrder.AdvanceToNextTurn();
			}
			return;
		}
		
		// Get available skills (not on cooldown)
		List<int> availableSkills = new List<int>();
		for (int i = 0; i < currentUnit.Skills.Length; i++)
		{
			if (currentUnit.CanUseSkill(i))
			{
				availableSkills.Add(i);
			}
		}
		
		// If no skills available, just attack or skip
		if (availableSkills.Count == 0)
		{
			Debug.Log(currentUnit.gameObject.name + " has no available skills!");
			if (turnOrder != null)
			{
				turnOrder.AdvanceToNextTurn();
			}
			return;
		}
		
		// Pick random skill
		int randomSkillIndex = availableSkills[Random.Range(0, availableSkills.Count)];
		
		// Pick random player unit target (already confirmed alive)
		Unit randomTarget = playerUnits[Random.Range(0, playerUnits.Count)];
		
		// Double-check target is still alive (in case it died between selection and use)
		if (randomTarget == null || !randomTarget.IsAlive())
		{
			Debug.LogWarning($"Target {randomTarget?.gameObject.name} is dead or null! Finding another target...");
			// Filter to only alive targets again
			playerUnits.RemoveAll(u => u == null || !u.IsAlive());
			
			if (playerUnits.Count == 0)
			{
				Debug.LogWarning("No alive targets remaining! Advancing turn.");
				if (turnOrder != null)
				{
					turnOrder.AdvanceToNextTurn();
				}
				return;
			}
			
			randomTarget = playerUnits[Random.Range(0, playerUnits.Count)];
		}
		
		// Start the turn (reduce cooldowns)
		currentUnit.StartTurn();
		
		// Hide action UI immediately to prevent input during skill execution
		HideActionUI();
		
		// Use the skill with delayed execution for proper animation timing
		StartCoroutine(ExecuteEnemySkillWithDelay(currentUnit, randomSkillIndex, randomTarget));
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
		string casterName = EventLogPanel.GetDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetDisplayNameForUnit(target) : "self";
		EventLogPanel.LogEvent($"{casterName} uses {skill.skillName} on {targetName}!");
		
		// Wait for skill animation
		yield return new WaitForSeconds(skillAnimationDelay);
		
		// Apply skill effects (which will log damage)
		caster.ApplySkillEffects(skill, target);
		
		// Wait for hit animation
		yield return new WaitForSeconds(hitAnimationDelay);
		
		// Show action UI again
		ShowActionUI();
		
		// Advance turn
		AdvanceToNextTurn();
	}
	
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
		string casterName = EventLogPanel.GetDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetDisplayNameForUnit(target) : "self";
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
		string casterName = EventLogPanel.GetDisplayNameForUnit(caster);
		string targetName = target != null ? EventLogPanel.GetDisplayNameForUnit(target) : "self";
		EventLogPanel.LogEvent($"{casterName} uses {skill.skillName} on {targetName}!");
		
		// Start coroutine for delayed effects
		StartCoroutine(ExecuteSkillWithDelayCoroutine(caster, skill, target, autoAdvanceTurn));
	}
	
	/// <summary>
	/// Coroutine to execute skill with proper timing delays
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
		
		// Apply skill effects (which will log damage)
		caster.ApplySkillEffects(skill, target);
		
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
	/// Coroutine to delay turn advancement for visual feedback (legacy, kept for compatibility)
	/// </summary>
	private System.Collections.IEnumerator DelayedAdvanceTurn()
	{
		yield return new WaitForSeconds(enemyTurnDelay);
		AdvanceToNextTurn();
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
	/// Gets the current unit whose turn it is
	/// </summary>
	public Unit GetCurrentUnit()
	{
		return currentUnit;
	}
	
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
		foreach (var unit in allUnits)
		{
			if (unit == null || !unit.IsAlive()) continue;
			
			// Check if unit is in creature spawn areas or enemy spawn areas
			int creatureIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: true);
			if (creatureIndex >= 0)
			{
				// Creature UI (indices 0-2)
				if (creatureNameTexts[creatureIndex] != null)
				{
					creatureNameTexts[creatureIndex].text = unit.UnitName;
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
				continue;
			}
			
			int enemyIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: false);
			if (enemyIndex >= 0)
			{
				// Enemy UI (indices 0-2)
				if (enemyNameTexts[enemyIndex] != null)
				{
					enemyNameTexts[enemyIndex].text = unit.UnitName;
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
	/// Called when all allies/player units are dead - opens the game over panel
	/// </summary>
	public void OnAllAlliesDead()
	{
		if (allAlliesDeadPanel != null)
		{
			allAlliesDeadPanel.SetActive(true);
		}
		else
		{
			Debug.LogWarning("GameManager: allAlliesDeadPanel is not assigned!");
		}
	}
	
	/// <summary>
	/// Called when the return to main menu button is clicked
	/// </summary>
	public void OnReturnToMainMenuClicked()
	{
		LoadMainMenuScene();
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
}
