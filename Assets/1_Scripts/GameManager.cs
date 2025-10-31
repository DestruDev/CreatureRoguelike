using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public TurnOrder turnOrder;

	[Header("Unit Info")]
    public TextMeshProUGUI UnitNameText;

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
        
        if (turnOrder != null)
        {
            Unit firstUnit = turnOrder.GetFirstUnit();
            if (firstUnit != null)
            {
                Debug.Log("GameManager: First unit determined - " + firstUnit.gameObject.name);
                SetCurrentUnit(firstUnit);
            // If first unit is enemy, delay processing slightly to ensure creatures exist
                if (firstUnit.IsEnemy)
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
		
		// If it's an enemy's turn, automatically process their turn
		if (currentUnit != null && currentUnit.IsEnemy)
		{
			ProcessEnemyTurn();
		}
	}
	
	/// <summary>
	/// Processes an enemy's turn: picks random skill and targets random creature
	/// </summary>
	private void ProcessEnemyTurn()
	{
		if (currentUnit == null || !currentUnit.IsEnemy)
			return;
		
		// Get all alive creatures (player units)
		Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		List<Unit> creatures = new List<Unit>();
		
		Debug.Log($"ProcessEnemyTurn: Found {allUnits.Length} total units");
		
		foreach (var unit in allUnits)
		{
			if (unit == null) continue;
			
			Debug.Log($"Checking unit: {unit.gameObject.name}, IsCreature: {unit.IsCreature}, IsAlive: {unit.IsAlive()}, unitType: {unit.unitType}");
			
			if (unit.IsCreature && unit.IsAlive())
			{
				creatures.Add(unit);
				Debug.Log($"Added creature: {unit.gameObject.name}");
			}
		}
		
		if (creatures.Count == 0)
		{
			Debug.LogWarning($"No creatures to target! Total units found: {allUnits.Length}. Checking all units:");
			foreach (var unit in allUnits)
			{
				if (unit != null)
				{
					Debug.LogWarning($"  - {unit.gameObject.name}: IsCreature={unit.IsCreature}, IsAlive={unit.IsAlive()}, unitType={unit.unitType}");
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
		
		// Pick random creature target
		Unit randomTarget = creatures[Random.Range(0, creatures.Count)];
		
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
				if (currentUnit.IsEnemy)
				{
					UnitNameText.gameObject.SetActive(false);
				}
				else
				{
					// Show text and update it for creature turns
					UnitNameText.gameObject.SetActive(true);
					UnitNameText.text = currentUnit.gameObject.name;
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
}
