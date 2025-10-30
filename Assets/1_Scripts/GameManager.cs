using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
}
