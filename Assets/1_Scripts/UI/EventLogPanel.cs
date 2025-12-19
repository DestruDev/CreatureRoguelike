using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages the event log UI display
/// Shows battle events like turn starts, skill usage, and damage
/// </summary>
public class EventLogPanel : MonoBehaviour
{
    [Header("Event Log UI")]
    [Tooltip("Event log display slots (oldest message at top, newest at bottom)")]
    public TextMeshProUGUI[] eventLogSlots = new TextMeshProUGUI[8];
    
    [Header("Settings")]
    [Tooltip("Maximum number of messages to keep in the log (automatically matches number of slots)")]
    [SerializeField] private int maxMessages = 8;
    
    private Queue<string> eventLog = new Queue<string>();
    private static EventLogPanel instance;
    
    private void Start()
    {
        // Set static instance for easy access
        if (instance == null)
        {
            instance = this;
        }
        
        // Auto-adjust maxMessages to match number of slots
        // This ensures we don't store more messages than we can display
        if (eventLogSlots != null && eventLogSlots.Length > 0)
        {
            maxMessages = eventLogSlots.Length;
        }
        
        // Clear all slots initially
        ClearLog();
    }
    
    /// <summary>
    /// Static method to log an event message
    /// </summary>
    public static void LogEvent(string message)
    {
        if (instance != null)
        {
            instance.AddLogMessage(message);
        }
        else
        {
            // Try to find instance if not set
            instance = FindFirstObjectByType<EventLogPanel>();
            if (instance != null)
            {
                instance.AddLogMessage(message);
            }
        }
    }
    
    /// <summary>
    /// Gets a display name for a unit, adding a distinguishing identifier if there are duplicates
    /// Similar to TurnOrderTimeline's GetDisplayNameForUnit method
    /// Uses a stable ordering that includes all units (alive and dead) to prevent letter reassignment when units die
    /// </summary>
    public static string GetDisplayNameForUnit(Unit unit)
    {
        if (unit == null)
            return "Unknown";
        
        string baseName = unit.UnitName;
        
        // Get ALL units in the scene (including inactive/dead ones) for stable letter assignment
        // This ensures letters don't change when units die
        Unit[] allUnitsIncludingInactive = Object.FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allUnitsIncludingInactive == null || allUnitsIncludingInactive.Length == 0)
            return baseName;
        
        // Count how many units have the same name (alive or dead)
        int sameNameCount = 0;
        foreach (var u in allUnitsIncludingInactive)
        {
            if (u != null && u.UnitName == baseName)
            {
                sameNameCount++;
            }
        }
        
        // If there are duplicates, add a distinguishing identifier
        if (sameNameCount > 1)
        {
            // Sort all units with same name (alive and dead) to get stable ordering
            List<Unit> sameNameUnits = new List<Unit>();
            foreach (var u in allUnitsIncludingInactive)
            {
                if (u != null && u.UnitName == baseName)
                {
                    sameNameUnits.Add(u);
                }
            }
            
            // Get TurnOrder for tiebreaker logic
            TurnOrder turnOrder = Object.FindFirstObjectByType<TurnOrder>();
            
            // Sort by team (player first), then by spawn index (if available), then by instance ID
            // This creates a stable ordering that doesn't change when units die
            sameNameUnits.Sort((a, b) =>
            {
                // Use tiebreaker logic for consistent ordering
                int tiebreak = turnOrder != null ? turnOrder.CompareUnitsForTiebreaker(a, b) : 0;
                if (tiebreak != 0)
                    return tiebreak;
                // Final fallback: instance ID (stable and unique)
                return a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            
            // Find this unit's index in the sorted list (stable position)
            for (int i = 0; i < sameNameUnits.Count; i++)
            {
                if (sameNameUnits[i] == unit)
                {
                    // Convert index to letter (0 = A, 1 = B, 2 = C, etc.)
                    // This letter is stable and won't change when other units die
                    char letter = (char)('A' + i);
                    return $"{baseName} {letter}";
                }
            }
        }
        
        // No duplicates or couldn't find in list, return base name
        return baseName;
    }
    
    /// <summary>
    /// Adds a message to the event log
    /// </summary>
    private void AddLogMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;
        
        // Add message to queue
        eventLog.Enqueue(message);
        
        // Remove oldest messages if we exceed max
        while (eventLog.Count > maxMessages)
        {
            eventLog.Dequeue();
        }
        
        // Update UI
        UpdateEventLogUI();
    }
    
    /// <summary>
    /// Updates the event log UI display
    /// </summary>
    private void UpdateEventLogUI()
    {
        if (eventLogSlots == null || eventLogSlots.Length == 0)
            return;
        
        // Convert queue to array for display (oldest first)
        string[] messages = eventLog.ToArray();
        
        // Initialize all slots first - keep them active and set to space to maintain height
        for (int i = 0; i < eventLogSlots.Length; i++)
        {
            if (eventLogSlots[i] != null)
            {
                eventLogSlots[i].text = " "; // Space character to maintain line height
                eventLogSlots[i].color = Color.white;
                eventLogSlots[i].gameObject.SetActive(true);
            }
        }
        
        // Fill slots: highest slot number (bottom physically) = newest, lower slots = older messages
        // Newest message goes to slot 7 (bottom), and older messages push upward
        
        // Start from the newest message (end of array) and work backwards
        int messageCount = Mathf.Min(messages.Length, eventLogSlots.Length);
        
        for (int i = 0; i < messageCount; i++)
        {
            // Slot 7 (bottom) gets the newest (last message)
            // Slot 6 gets second newest, etc.
            int messageIndex = messages.Length - 1 - i;
            int slotIndex = eventLogSlots.Length - 1 - i; // Reverse: highest slot gets newest
            
            if (messageIndex >= 0 && slotIndex >= 0 && eventLogSlots[slotIndex] != null)
            {
                eventLogSlots[slotIndex].text = messages[messageIndex];
                eventLogSlots[slotIndex].color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Clears the event log
    /// </summary>
    public void ClearLog()
    {
        eventLog.Clear();
        // Set all slots to space to maintain height
        for (int i = 0; i < eventLogSlots.Length; i++)
        {
            if (eventLogSlots[i] != null)
            {
                eventLogSlots[i].text = " ";
                eventLogSlots[i].color = Color.white;
                eventLogSlots[i].gameObject.SetActive(true);
            }
        }
    }
}

