using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TurnOrder turnOrder;

	[Header("Unit Info")]
    public TextMeshProUGUI UnitNameText;
    
	[Header("Creature Status UI (3 units)")]
    [Tooltip("Creature name text displays (index 0-2 correspond to creature 1-3)")]
    public TextMeshProUGUI[] creatureNameTexts = new TextMeshProUGUI[3];
    
    [Tooltip("Creature health bar fill images (index 0-2 correspond to creature 1-3)")]
    public UnityEngine.UI.Image[] creatureHealthFills = new UnityEngine.UI.Image[3];
    
    [Header("Enemy Status UI (3 units)")]
    [Tooltip("Enemy name text displays (index 0-2 correspond to enemy 1-3)")]
    public TextMeshProUGUI[] enemyNameTexts = new TextMeshProUGUI[3];
    
    [Tooltip("Enemy health bar fill images (index 0-2 correspond to enemy 1-3)")]
    public UnityEngine.UI.Image[] enemyHealthFills = new UnityEngine.UI.Image[3];
    
    private TurnOrder turnOrderRef; // Reference to get spawn indices

    [Header("Turn Management")]
    private Unit currentUnit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find TurnOrder if not assigned
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
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
            Unit firstUnit = turnOrder.GetFirstUnit();
            if (firstUnit != null)
            {
                Debug.Log("GameManager: First unit determined - " + firstUnit.gameObject.name);
                SetCurrentUnit(firstUnit);
            // If first unit is enemy, delay processing slightly to ensure player units exist
                if (firstUnit.IsEnemyUnit)
                {
                    yield return new WaitForSeconds(0.1f);
                }
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
		// Temporary: damage all units by 20 when pressing Q
		if (Input.GetKeyDown(KeyCode.Q))
		{
			DamageAllUnits(20);
		}
    }

	// Temporary helper to damage all units in the scene
	private void DamageAllUnits(int amount)
	{
		Unit[] units = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		foreach (var unit in units)
		{
			unit.TakeDamage(amount);
		}
		Debug.Log($"Damaged {units.Length} units for {amount}");
	}

	/// <summary>
	/// Sets the current unit whose turn it is and updates the UI
	/// </summary>
	public void SetCurrentUnit(Unit unit)
	{
		currentUnit = unit;
		UpdateUnitNameText();
		UpdateSkillPanel();
		
		// If it's an enemy's turn (based on spawn area assignment), automatically process their turn
		if (currentUnit != null && currentUnit.IsEnemyUnit)
		{
			ProcessEnemyTurn();
		}
	}
	
	/// <summary>
	/// Processes an enemy's turn: picks random skill and targets random player unit
	/// </summary>
	private void ProcessEnemyTurn()
	{
		if (currentUnit == null || !currentUnit.IsEnemyUnit)
			return;
		
		// Get all alive player units (targetable by enemies)
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		List<Unit> playerUnits = new List<Unit>();
		
		Debug.Log($"ProcessEnemyTurn: Found {allUnits.Length} total units");
		
		foreach (var unit in allUnits)
		{
			if (unit == null) continue;
			
			Debug.Log($"Checking unit: {unit.gameObject.name}, IsPlayerUnit: {unit.IsPlayerUnit}, IsAlive: {unit.IsAlive()}");
			
			if (unit.IsPlayerUnit && unit.IsAlive())
			{
				playerUnits.Add(unit);
				Debug.Log($"Added player unit: {unit.gameObject.name}");
			}
		}
		
		if (playerUnits.Count == 0)
		{
			Debug.LogWarning($"No player units to target! Total units found: {allUnits.Length}. Checking all units:");
			foreach (var unit in allUnits)
			{
				if (unit != null)
				{
					Debug.LogWarning($"  - {unit.gameObject.name}: IsPlayerUnit={unit.IsPlayerUnit}, IsAlive={unit.IsAlive()}");
				}
			}
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
		
		// Pick random player unit target
		Unit randomTarget = playerUnits[Random.Range(0, playerUnits.Count)];
		
		// Start the turn (reduce cooldowns)
		currentUnit.StartTurn();
		
		// Use the skill
		currentUnit.UseSkill(randomSkillIndex, randomTarget);
		
		// Small delay before advancing to next turn (optional, for visual feedback)
		Invoke(nameof(AdvanceToNextTurn), 1f);
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
	/// Updates the UnitNameText UI with the current unit's name
	/// </summary>
	private void UpdateUnitNameText()
	{
		if (UnitNameText != null)
		{
			if (currentUnit != null)
			{
				// Hide text during enemy turns
				if (currentUnit.IsEnemyUnit)
				{
					UnitNameText.gameObject.SetActive(false);
				}
				else
				{
					// Show text and update it for creature turns using ScriptableObject name
					UnitNameText.gameObject.SetActive(true);
					UnitNameText.text = currentUnit.UnitName;
				}
			}
			else
			{
				UnitNameText.gameObject.SetActive(false);
			}
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
		}
	}
}
