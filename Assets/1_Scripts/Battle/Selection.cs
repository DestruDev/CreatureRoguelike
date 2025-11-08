using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Handles selection of units, skills, items, and other menu elements.
/// Provides cycling functionality for navigating through selectable items.
/// </summary>
public class Selection : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable wrapping - when reaching the end, wrap to beginning and vice versa")]
    public bool wrapAround = true;
    
    [Tooltip("Allow selecting nothing (selection index = -1)")]
    public bool allowNoSelection = false;

    [Header("Creature Highlight Markers")]
    [Tooltip("Sprite renderers for highlighting creature units (player units)")]
    public SpriteRenderer creatureHighlightMarker1;
    public SpriteRenderer creatureHighlightMarker2;
    public SpriteRenderer creatureHighlightMarker3;

    [Header("Enemy Highlight Markers")]
    [Tooltip("Sprite renderers for highlighting enemy units")]
    public SpriteRenderer enemyHighlightMarker1;
    public SpriteRenderer enemyHighlightMarker2;
    public SpriteRenderer enemyHighlightMarker3;

    [Header("Highlight Colors")]
    [Tooltip("Color for creature highlight markers")]
    public Color creatureHighlightColor = Color.white;
    
    [Tooltip("Transparency/Alpha for creature highlight markers (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float creatureHighlightAlpha = 1f;
    
    [Tooltip("Color for enemy highlight markers")]
    public Color enemyHighlightColor = Color.white;
    
    [Tooltip("Transparency/Alpha for enemy highlight markers (0 = fully transparent, 1 = fully opaque)")]
    [Range(0f, 1f)]
    public float enemyHighlightAlpha = 1f;
    
    // Inspect mode color override (null when not in inspect mode)
    private Color? inspectModeColor = null;

    [Header("UI Selection Markers")]
    [Tooltip("GameObjects for highlighting UI button selections")]
    public GameObject SelectMarker1;
    public GameObject SelectMarker2;

    [Header("Mouse Hover Settings")]
    [Tooltip("Maximum distance for mouse hover detection (in world units)")]
    public float hoverDetectionDistance = 2f;
    
    [Tooltip("Camera to use for raycasting (if null, uses Camera.main)")]
    public Camera raycastCamera;

    // Current selection index (-1 means no selection)
    private int currentIndex = -1;
    
    // List of currently selectable items (can be Units, Skills, Items, etc.)
    private List<object> selectableItems = new List<object>();
    
    // Selection type to determine behavior
    private SelectionType currentSelectionType = SelectionType.Units;
    
    // Highlight markers array
    private SpriteRenderer[] highlightMarkers;
    
    // Mapping of units to their highlight marker indices
    private Dictionary<Unit, int> unitToMarkerIndex = new Dictionary<Unit, int>();
    
    // Currently hovered unit (from mouse)
    private Unit hoveredUnit = null;
    
    // Currently selected unit (from keyboard navigation)
    private Unit selectedUnit = null;

    // Events
    public event Action<object> OnSelectionChanged; // Called when selection changes (passes selected item)
    public event Action<int> OnIndexChanged; // Called when selection index changes
    
    /// <summary>
    /// Gets the current selection index (-1 if no selection)
    /// </summary>
    public int CurrentIndex => currentIndex;
    
    /// <summary>
    /// Gets the currently selected item (null if no selection)
    /// </summary>
    public object CurrentSelection => IsValidSelection() ? selectableItems[currentIndex] : null;
    
    /// <summary>
    /// Gets the number of selectable items
    /// </summary>
    public int Count => selectableItems.Count;
    
    /// <summary>
    /// Checks if there's a valid selection
    /// </summary>
    public bool IsValidSelection()
    {
        return currentIndex >= 0 && currentIndex < selectableItems.Count;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize highlight markers array
        // Indices 0-2: Creatures, Indices 3-5: Enemies
        highlightMarkers = new SpriteRenderer[]
        {
            creatureHighlightMarker1, creatureHighlightMarker2, creatureHighlightMarker3,
            enemyHighlightMarker1, enemyHighlightMarker2, enemyHighlightMarker3
        };
        
        // Initially disable all highlight markers
        foreach (var marker in highlightMarkers)
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }
        
        // Initially disable UI selection markers
        if (SelectMarker1 != null)
        {
            SelectMarker1.SetActive(false);
        }
        if (SelectMarker2 != null)
        {
            SelectMarker2.SetActive(false);
        }
        
        // Find camera if not assigned
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
        
        // Build unit to marker index mapping
        BuildUnitMarkerMapping();
    }

    // Update is called once per frame
    void Update()
    {
        // Only update hover highlights if we're in unit selection mode
        if (currentSelectionType == SelectionType.Units)
        {
            UpdateMouseHoverHighlight();
        }
        else
        {
            // Clear hover when not in unit selection mode
            if (hoveredUnit != null)
            {
                ClearHoverHighlight();
            }
        }
    }

    #region Unit Selection

    /// <summary>
    /// Sets up selection for units (allies, enemies, or both)
    /// </summary>
    /// <param name="targetType">Type of units to select from</param>
    /// <param name="caster">The unit casting/using the skill/item (for target validation)</param>
    /// <param name="skill">Optional skill to validate targets against</param>
    public void SetupUnitSelection(UnitTargetType targetType, Unit caster = null, Skill skill = null)
    {
        List<Unit> validUnits = GetValidUnits(targetType, caster, skill);
        
        // Rebuild unit marker mapping BEFORE setting selection (so highlights can work immediately)
        RebuildUnitMarkerMapping();
        
        // Clear any existing hover (but not keyboard selection)
        hoveredUnit = null;
        
        // Set the selection (this will trigger NotifySelectionChanged which will highlight the first unit)
        SetSelection(validUnits.ToArray(), SelectionType.Units);
    }

    /// <summary>
    /// Gets valid units based on target type and optional skill requirements
    /// Units are sorted by spawn index for consistent ordering
    /// </summary>
    private List<Unit> GetValidUnits(UnitTargetType targetType, Unit caster = null, Skill skill = null)
    {
        List<Unit> validUnits = new List<Unit>();
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;

            bool isValid = false;

            switch (targetType)
            {
                case UnitTargetType.Allies:
                    isValid = caster != null && unit.IsPlayerUnit == caster.IsPlayerUnit && unit != caster;
                    break;
                
                case UnitTargetType.Enemies:
                    isValid = caster != null && unit.IsPlayerUnit != caster.IsPlayerUnit;
                    break;
                
                case UnitTargetType.AllAllies:
                    isValid = unit.IsPlayerUnit;
                    break;
                
                case UnitTargetType.AllEnemies:
                    isValid = unit.IsEnemyUnit;
                    break;
                
                case UnitTargetType.Any:
                    isValid = true;
                    break;
                
                case UnitTargetType.Self:
                    isValid = caster != null && unit == caster;
                    break;
                
                case UnitTargetType.AnyAlive:
                    isValid = unit.IsAlive();
                    break;
            }

            // If a skill is provided, also validate against skill's targeting requirements
            if (isValid && skill != null && caster != null)
            {
                isValid = skill.CanTarget(unit, caster);
            }

            if (isValid)
            {
                validUnits.Add(unit);
            }
        }

        // Sort units by spawn index for consistent ordering
        // This ensures the same order every time and that the first enemy is always first when selecting enemies
        validUnits.Sort((unit1, unit2) =>
        {
            int index1 = GetUnitSpawnIndex(unit1);
            int index2 = GetUnitSpawnIndex(unit2);
            
            // If both have valid spawn indices, sort by index (lower = earlier)
            if (index1 >= 0 && index2 >= 0)
            {
                return index1.CompareTo(index2);
            }
            
            // If only one has a valid index, it comes first
            if (index1 >= 0) return -1;
            if (index2 >= 0) return 1;
            
            // If neither has a valid index, use instance ID for deterministic ordering
            return unit1.GetInstanceID().CompareTo(unit2.GetInstanceID());
        });

        return validUnits;
    }

    /// <summary>
    /// Gets the currently selected unit (convenience method)
    /// </summary>
    public Unit GetSelectedUnit()
    {
        if (currentSelectionType == SelectionType.Units && CurrentSelection is Unit unit)
        {
            return unit;
        }
        return null;
    }

    #endregion

    #region Skill Selection

    /// <summary>
    /// Sets up selection for skills from a unit
    /// </summary>
    /// <param name="unit">Unit whose skills to select from</param>
    /// <param name="onlyAvailable">If true, only show skills that can be used (not on cooldown)</param>
    public void SetupSkillSelection(Unit unit, bool onlyAvailable = false)
    {
        if (unit == null || unit.Skills == null)
        {
            SetSelection(new Skill[0], SelectionType.Skills);
            return;
        }

        List<Skill> validSkills = new List<Skill>();
        for (int i = 0; i < unit.Skills.Length; i++)
        {
            if (unit.Skills[i] == null)
                continue;

            if (!onlyAvailable || unit.CanUseSkill(i))
            {
                validSkills.Add(unit.Skills[i]);
            }
        }

        SetSelection(validSkills.ToArray(), SelectionType.Skills);
    }

    /// <summary>
    /// Gets the currently selected skill (convenience method)
    /// </summary>
    public Skill GetSelectedSkill()
    {
        if (currentSelectionType == SelectionType.Skills && CurrentSelection is Skill skill)
        {
            return skill;
        }
        return null;
    }

    /// <summary>
    /// Gets the index of the selected skill in the unit's skill array (accounting for only showing available skills)
    /// </summary>
    public int GetSelectedSkillIndex(Unit unit, bool onlyAvailable = false)
    {
        if (unit == null || currentSelectionType != SelectionType.Skills)
            return -1;

        Skill selectedSkill = GetSelectedSkill();
        if (selectedSkill == null)
            return -1;

        if (onlyAvailable)
        {
            // Find the index in the available skills
            int availableIndex = 0;
            for (int i = 0; i < unit.Skills.Length; i++)
            {
                if (unit.Skills[i] == null)
                    continue;

                if (unit.CanUseSkill(i))
                {
                    if (unit.Skills[i] == selectedSkill)
                    {
                        return i; // Return the actual skill index in the unit's array
                    }
                    availableIndex++;
                }
            }
        }
        else
        {
            // Find the index in all skills
            for (int i = 0; i < unit.Skills.Length; i++)
            {
                if (unit.Skills[i] == selectedSkill)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    #endregion

    #region Generic Selection

    /// <summary>
    /// Sets up selection with an array of any objects
    /// </summary>
    public void SetSelection<T>(T[] items, SelectionType type = SelectionType.Generic) where T : class
    {
        selectableItems.Clear();
        
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    selectableItems.Add(item);
                }
            }
        }

        currentSelectionType = type;

        // Reset selection index
        if (selectableItems.Count > 0)
        {
            currentIndex = 0;
        }
        else if (allowNoSelection)
        {
            currentIndex = -1;
        }
        else
        {
            currentIndex = 0;
        }

        NotifySelectionChanged();
    }

    /// <summary>
    /// Clears the current selection
    /// </summary>
    public void ClearSelection()
    {
        selectableItems.Clear();
        currentIndex = -1;
        currentSelectionType = SelectionType.None;
        
        // Clear both hover and keyboard selection highlights
        ClearHoverHighlight();
        
        NotifySelectionChanged();
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Moves selection forward (next item)
    /// </summary>
    public void Next()
    {
        if (selectableItems.Count == 0)
            return;

        if (currentIndex < 0 && allowNoSelection)
        {
            currentIndex = 0;
            NotifySelectionChanged();
            return;
        }

        if (currentIndex >= selectableItems.Count - 1)
        {
            if (wrapAround)
            {
                currentIndex = 0;
            }
            // If no wrap, do nothing
        }
        else
        {
            currentIndex++;
        }

        NotifySelectionChanged();
    }

    /// <summary>
    /// Moves selection backward (previous item)
    /// </summary>
    public void Previous()
    {
        if (selectableItems.Count == 0)
            return;

        if (currentIndex < 0 && allowNoSelection)
        {
            currentIndex = selectableItems.Count - 1;
            NotifySelectionChanged();
            return;
        }

        if (currentIndex <= 0)
        {
            if (wrapAround)
            {
                currentIndex = selectableItems.Count - 1;
            }
            // If no wrap, do nothing
        }
        else
        {
            currentIndex--;
        }

        NotifySelectionChanged();
    }

    /// <summary>
    /// Sets the selection to a specific index
    /// </summary>
    public void SetIndex(int index)
    {
        if (selectableItems.Count == 0)
        {
            if (allowNoSelection && index == -1)
            {
                currentIndex = -1;
                NotifySelectionChanged();
            }
            return;
        }

        if (index < 0)
        {
            if (allowNoSelection)
            {
                currentIndex = -1;
                NotifySelectionChanged();
            }
            return;
        }

        if (index >= selectableItems.Count)
        {
            if (wrapAround)
            {
                currentIndex = index % selectableItems.Count;
            }
            else
            {
                currentIndex = selectableItems.Count - 1;
            }
        }
        else
        {
            currentIndex = index;
        }

        NotifySelectionChanged();
    }

    /// <summary>
    /// Sets selection by finding a specific object in the list
    /// </summary>
    public bool SelectItem(object item)
    {
        for (int i = 0; i < selectableItems.Count; i++)
        {
            if (selectableItems[i] == item)
            {
                currentIndex = i;
                NotifySelectionChanged();
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Events

    /// <summary>
    /// Notifies listeners that the selection has changed
    /// </summary>
    private void NotifySelectionChanged()
    {
        OnIndexChanged?.Invoke(currentIndex);
        
        Unit previousSelectedUnit = selectedUnit;
        
        if (IsValidSelection())
        {
            // Update selected unit
            if (currentSelectionType == SelectionType.Units && selectableItems[currentIndex] is Unit unit)
            {
                selectedUnit = unit;
            }
            else
            {
                selectedUnit = null;
            }
            
            OnSelectionChanged?.Invoke(selectableItems[currentIndex]);
        }
        else
        {
            selectedUnit = null;
            OnSelectionChanged?.Invoke(null);
        }
        
        // Update highlight markers for keyboard-selected unit
        if (currentSelectionType == SelectionType.Units)
        {
            UpdateKeyboardSelectionHighlight(previousSelectedUnit, selectedUnit);
        }
    }
    
    /// <summary>
    /// Updates highlight markers based on keyboard selection
    /// </summary>
    private void UpdateKeyboardSelectionHighlight(Unit previousUnit, Unit newUnit)
    {
        // Clear previous selection highlight (only if it's different from the new one and not being hovered)
        if (previousUnit != null && previousUnit != newUnit && previousUnit != hoveredUnit)
        {
            SetHighlightMarkerForUnit(previousUnit, false);
        }
        
        // Set new selection highlight
        // If it's being hovered, the hover highlight will handle it, but we still want it active
        // So we always set it to true for the selected unit
        if (newUnit != null)
        {
            SetHighlightMarkerForUnit(newUnit, true);
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Refreshes the selection list, removing any invalid items (e.g., dead units)
    /// </summary>
    public void RefreshSelection()
    {
        if (currentSelectionType == SelectionType.Units)
        {
            // Remove dead units
            selectableItems.RemoveAll(item => 
            {
                if (item is Unit unit)
                {
                    return !unit.IsAlive();
                }
                return false;
            });

            // Adjust index if current selection became invalid
            if (currentIndex >= selectableItems.Count && selectableItems.Count > 0)
            {
                currentIndex = selectableItems.Count - 1;
            }
            else if (selectableItems.Count == 0)
            {
                currentIndex = allowNoSelection ? -1 : 0;
            }

            NotifySelectionChanged();
        }
        // Could add refresh logic for other selection types here
    }

    /// <summary>
    /// Gets all selectable items as an array
    /// </summary>
    public object[] GetAllItems()
    {
        return selectableItems.ToArray();
    }

    /// <summary>
    /// Gets all selectable items as a list
    /// </summary>
    public List<object> GetAllItemsList()
    {
        return new List<object>(selectableItems);
    }

    #endregion

    #region Mouse Hover Highlighting

    /// <summary>
    /// Builds a mapping between units and their highlight marker indices
    /// </summary>
    private void BuildUnitMarkerMapping()
    {
        unitToMarkerIndex.Clear();
        
        // Get all units in the scene
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        // Find TurnOrder to get spawn indices (similar to how GameManager does it)
        TurnOrder turnOrder = FindFirstObjectByType<TurnOrder>();
        if (turnOrder == null)
        {
            Debug.LogWarning("Selection: TurnOrder not found. Cannot build unit marker mapping.");
            return;
        }
        
        // Map units to indices (0-5) based on spawn position
        for (int i = 0; i < allUnits.Length; i++)
        {
            Unit unit = allUnits[i];
            if (unit == null) continue;
            
            // Try to get spawn index from TurnOrder
            int spawnIndex = GetUnitSpawnIndex(unit);
            if (spawnIndex >= 0 && spawnIndex < 6)
            {
                unitToMarkerIndex[unit] = spawnIndex;
            }
        }
    }

    /// <summary>
    /// Gets the spawn index (0-5) for a unit
    /// </summary>
    private int GetUnitSpawnIndex(Unit unit)
    {
        if (unit == null) return -1;
        
        // Use reflection similar to GameManager to get spawn area index
        Spawning spawning = FindFirstObjectByType<Spawning>();
        if (spawning == null) return -1;
        
        // Check creature spawn areas (indices 0-2)
        int creatureIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: true, spawning);
        if (creatureIndex >= 0)
        {
            return creatureIndex; // 0-2 for creatures
        }
        
        // Check enemy spawn areas (indices 3-5)
        int enemyIndex = GetUnitSpawnAreaIndexInArray(unit, isCreature: false, spawning);
        if (enemyIndex >= 0)
        {
            return enemyIndex + 3; // 3-5 for enemies (offset by 3)
        }
        
        return -1;
    }

    /// <summary>
    /// Gets the spawn area index within the creature or enemy array
    /// </summary>
    private int GetUnitSpawnAreaIndexInArray(Unit unit, bool isCreature, Spawning spawning)
    {
        if (unit == null || spawning == null) return -1;
        
        var spawnAreasField = typeof(Spawning).GetField(isCreature ? "creatureSpawnAreas" : "enemySpawnAreas",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (spawnAreasField != null)
        {
            var spawnAreas = spawnAreasField.GetValue(spawning) as Transform[];
            if (spawnAreas != null)
            {
                for (int i = 0; i < spawnAreas.Length; i++)
                {
                    if (spawnAreas[i] != null)
                    {
                        Transform unitTransform = unit.transform;
                        while (unitTransform != null)
                        {
                            if (unitTransform == spawnAreas[i])
                            {
                                return i;
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
    /// Updates mouse hover highlight based on which unit the mouse is over
    /// </summary>
    private void UpdateMouseHoverHighlight()
    {
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
            if (raycastCamera == null)
                return;
        }
        
        Unit newHoveredUnit = GetUnitUnderMouse();
        
        // If hovered unit changed, update highlights
        if (newHoveredUnit != hoveredUnit)
        {
            // Clear previous hover (only if it's not the currently selected unit)
            if (hoveredUnit != null && hoveredUnit != selectedUnit)
            {
                SetHighlightMarkerForUnit(hoveredUnit, false);
            }
            
            hoveredUnit = newHoveredUnit;
            
            // Set new hover (only if it's not already selected via keyboard)
            if (hoveredUnit != null)
            {
                // Only highlight if this unit is in the selectable items list
                if (IsUnitSelectable(hoveredUnit))
                {
                    // Only highlight if it's not already highlighted by keyboard selection
                    if (hoveredUnit != selectedUnit)
                    {
                        SetHighlightMarkerForUnit(hoveredUnit, true);
                    }
                }
                else
                {
                    hoveredUnit = null; // Don't highlight non-selectable units
                }
            }
        }
        
        // Make sure the currently selected unit is always highlighted
        if (selectedUnit != null)
        {
            SetHighlightMarkerForUnit(selectedUnit, true);
        }
    }

    /// <summary>
    /// Gets the unit that the mouse is currently hovering over
    /// </summary>
    private Unit GetUnitUnderMouse()
    {
        if (raycastCamera == null)
            return null;
        
        // Convert mouse position to world position
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = raycastCamera.nearClipPlane + 1f; // Set appropriate distance
        Vector3 worldPos = raycastCamera.ScreenToWorldPoint(mousePos);
        
        // Raycast from camera through mouse position
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Try physics raycast first (if units have colliders)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null && unit.IsAlive())
            {
                return unit;
            }
        }
        
        // Fallback: Distance-based check for units
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Unit closestUnit = null;
        float closestDistance = hoverDetectionDistance;
        
        // Project mouse to ground plane (assuming units are at y=0 or similar)
        Vector3 mouseWorldPos = raycastCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, raycastCamera.nearClipPlane));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            
            foreach (var unit in allUnits)
            {
                if (unit == null || !unit.IsAlive())
                    continue;
                
                float distToUnit = Vector3.Distance(hitPoint, unit.transform.position);
                if (distToUnit < closestDistance)
                {
                    closestDistance = distToUnit;
                    closestUnit = unit;
                }
            }
        }
        
        return closestUnit;
    }

    /// <summary>
    /// Checks if a unit is in the selectable items list
    /// </summary>
    private bool IsUnitSelectable(Unit unit)
    {
        if (unit == null)
            return false;
        
        foreach (var item in selectableItems)
        {
            if (item is Unit u && u == unit)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets the highlight marker for a unit active or inactive
    /// </summary>
    private void SetHighlightMarkerForUnit(Unit unit, bool active)
    {
        if (unit == null || !unitToMarkerIndex.ContainsKey(unit))
            return;
        
        int markerIndex = unitToMarkerIndex[unit];
        if (markerIndex >= 0 && markerIndex < highlightMarkers.Length)
        {
            SpriteRenderer marker = highlightMarkers[markerIndex];
            if (marker != null)
            {
                marker.gameObject.SetActive(active);
                
                // Apply color based on whether it's a creature (0-2) or enemy (3-5) marker
                if (active)
                {
                    // Use inspect mode color if set, otherwise use normal colors
                    if (inspectModeColor.HasValue)
                    {
                        Color colorWithAlpha = inspectModeColor.Value;
                        marker.color = colorWithAlpha;
                    }
                    else
                    {
                        if (markerIndex < 3)
                        {
                            // Creature marker (indices 0-2)
                            Color colorWithAlpha = creatureHighlightColor;
                            colorWithAlpha.a = creatureHighlightAlpha;
                            marker.color = colorWithAlpha;
                        }
                        else
                        {
                            // Enemy marker (indices 3-5)
                            Color colorWithAlpha = enemyHighlightColor;
                            colorWithAlpha.a = enemyHighlightAlpha;
                            marker.color = colorWithAlpha;
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Sets the inspect mode color override (used when in inspect mode)
    /// </summary>
    public void SetInspectModeColor(Color color, float alpha)
    {
        Color colorWithAlpha = color;
        colorWithAlpha.a = alpha;
        inspectModeColor = colorWithAlpha;
        
        // Update all currently active markers to use the inspect color
        foreach (var marker in highlightMarkers)
        {
            if (marker != null && marker.gameObject.activeSelf)
            {
                marker.color = colorWithAlpha;
            }
        }
    }
    
    /// <summary>
    /// Clears the inspect mode color override (restores normal colors)
    /// </summary>
    public void ClearInspectModeColor()
    {
        inspectModeColor = null;
        
        // Update all currently active markers to use normal colors
        foreach (var marker in highlightMarkers)
        {
            if (marker != null && marker.gameObject.activeSelf)
            {
                int markerIndex = System.Array.IndexOf(highlightMarkers, marker);
                if (markerIndex >= 0)
                {
                    if (markerIndex < 3)
                    {
                        Color colorWithAlpha = creatureHighlightColor;
                        colorWithAlpha.a = creatureHighlightAlpha;
                        marker.color = colorWithAlpha;
                    }
                    else
                    {
                        Color colorWithAlpha = enemyHighlightColor;
                        colorWithAlpha.a = enemyHighlightAlpha;
                        marker.color = colorWithAlpha;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clears the hover highlight
    /// </summary>
    private void ClearHoverHighlight()
    {
        if (hoveredUnit != null && hoveredUnit != selectedUnit)
        {
            SetHighlightMarkerForUnit(hoveredUnit, false);
        }
        hoveredUnit = null;
        
        // Also clear keyboard selection highlight
        if (selectedUnit != null)
        {
            SetHighlightMarkerForUnit(selectedUnit, false);
            selectedUnit = null;
        }
    }

    /// <summary>
    /// Rebuilds the unit marker mapping (call this when units are spawned or destroyed)
    /// </summary>
    public void RebuildUnitMarkerMapping()
    {
        BuildUnitMarkerMapping();
    }

    #endregion

    #region UI Selection Markers

    /// <summary>
    /// Shows or hides the UI selection marker at the specified index (0 or 1)
    /// </summary>
    /// <param name="markerIndex">Index of the marker (0 for SelectMarker1, 1 for SelectMarker2)</param>
    /// <param name="active">Whether to show (true) or hide (false) the marker</param>
    public void SetUISelectionMarker(int markerIndex, bool active)
    {
        GameObject marker = null;
        if (markerIndex == 0)
        {
            marker = SelectMarker1;
        }
        else if (markerIndex == 1)
        {
            marker = SelectMarker2;
        }

        if (marker != null)
        {
            marker.SetActive(active);
        }
    }

    /// <summary>
    /// Hides all UI selection markers
    /// </summary>
    public void HideAllUISelectionMarkers()
    {
        if (SelectMarker1 != null)
        {
            SelectMarker1.SetActive(false);
        }
        if (SelectMarker2 != null)
        {
            SelectMarker2.SetActive(false);
        }
    }

    #endregion
}

#region Enums

/// <summary>
/// Type of units to select from
/// </summary>
public enum UnitTargetType
{
    Allies,         // Other allies (not self)
    Enemies,        // Enemy units
    AllAllies,      // All ally units (including self)
    AllEnemies,     // All enemy units
    Any,            // Any unit
    Self,           // Only the caster
    AnyAlive        // Any alive unit
}

/// <summary>
/// Type of selection currently active
/// </summary>
public enum SelectionType
{
    None,
    Units,
    Skills,
    Items,
    UIButtons,
    Generic
}

#endregion
