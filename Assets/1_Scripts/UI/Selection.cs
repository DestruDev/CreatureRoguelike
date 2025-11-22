using UnityEngine;
using UnityEngine.UI;
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

    [Header("Simple Selection Marker")]
    [Tooltip("UI Image prefab to instantiate as a selection marker (should be a square UI Image)")]
    public GameObject SimpleSelectionMarker;
    
    [Tooltip("Canvas to parent the selection markers to (if null, will try to find one automatically)")]
    public Canvas markerCanvas;
    
    [Tooltip("GameObject that the selection marker will spawn under when SetMarkerParent is called (null to use default canvas)")]
    public GameObject markerParent;
    
    [Tooltip("Color of the selection marker")]
    public Color markerColor = Color.white;
    
    [Tooltip("Alpha (transparency) value of the selection marker (0-1)")]
    [Range(0f, 1f)]
    public float markerAlpha = 1f;

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
    
    // Currently hovered unit (from mouse)
    private Unit hoveredUnit = null;
    
    // Currently selected unit (from keyboard navigation)
    private Unit selectedUnit = null;
    
    // Store last selected units by target type (for restoring selection when re-entering selection mode)
    private Unit lastSelectedAlly = null;
    private Unit lastSelectedEnemy = null;
    
    // Dictionary to track instantiated selection markers for each selectable item
    private Dictionary<object, GameObject> selectionMarkers = new Dictionary<object, GameObject>();
    
    // Track whether markers should render in front (default) or behind UI
    private bool markersRenderInFront = true;
    
    // Optional custom parent for markers (overrides markerCanvas when set)
    private Transform customMarkerParent = null;
    
    // Flag to disable navigation (e.g., when settings panel is open)
    private bool navigationDisabled = false;

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
        // Find camera if not assigned
        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
        
        // Find canvas if not assigned
        if (markerCanvas == null)
        {
            markerCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only update hover highlights if we're in unit selection mode
        if (currentSelectionType == SelectionType.Units)
        {
            UpdateMouseHoverHighlight();
            
            // Handle mouse click selection for units
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClickSelection();
            }
        }
        else
        {
            // Clear hover when not in unit selection mode
            if (hoveredUnit != null)
            {
                ClearHoverHighlight();
            }
        }
        
        // Update simple selection marker position (in case object moved)
        if (SimpleSelectionMarker != null && IsValidSelection())
        {
            object selectedItem = selectableItems[currentIndex];
            if (selectedItem != null && selectionMarkers.ContainsKey(selectedItem))
            {
                UpdateSimpleSelectionMarker(selectedItem);
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
    public void SetupUnitSelection(UnitTargetType targetType, Unit caster = null, Skill skill = null, Item item = null)
    {
        List<Unit> validUnits = GetValidUnits(targetType, caster, skill, item);
        
        // Clear any existing hover (but not keyboard selection)
        hoveredUnit = null;
        
        // Set the selection (this will trigger NotifySelectionChanged which will highlight the first unit)
        SetSelection(validUnits.ToArray(), SelectionType.Units);
        
        // Restore previously selected unit based on target type (allies vs enemies)
        Unit unitToRestore = null;
        if (caster != null)
        {
            // Determine if we're selecting allies or enemies
            bool isSelectingAllies = targetType == UnitTargetType.Allies || 
                                     targetType == UnitTargetType.AllAllies || 
                                     targetType == UnitTargetType.Self;
            bool isSelectingEnemies = targetType == UnitTargetType.Enemies || 
                                      targetType == UnitTargetType.AllEnemies;
            
            // For "Any" or "AnyAlive", determine based on what units are actually in the list
            if (targetType == UnitTargetType.Any || targetType == UnitTargetType.AnyAlive)
            {
                // Check if we have a stored unit that matches the caster's team
                if (lastSelectedAlly != null && lastSelectedAlly.IsPlayerUnit == caster.IsPlayerUnit && validUnits.Contains(lastSelectedAlly))
                {
                    unitToRestore = lastSelectedAlly;
                }
                else if (lastSelectedEnemy != null && lastSelectedEnemy.IsPlayerUnit != caster.IsPlayerUnit && validUnits.Contains(lastSelectedEnemy))
                {
                    unitToRestore = lastSelectedEnemy;
                }
            }
            else if (isSelectingAllies && lastSelectedAlly != null && validUnits.Contains(lastSelectedAlly))
            {
                unitToRestore = lastSelectedAlly;
            }
            else if (isSelectingEnemies && lastSelectedEnemy != null && validUnits.Contains(lastSelectedEnemy))
            {
                unitToRestore = lastSelectedEnemy;
            }
        }
        
        // Restore the unit if we found one
        if (unitToRestore != null && IsValidSelection())
        {
            SelectItem(unitToRestore);
        }
    }
    
    /// <summary>
    /// Stores the currently selected unit based on target type (for restoring later)
    /// Should be called when backing out of selection mode
    /// </summary>
    /// <param name="caster">The unit that was casting/using the skill/item</param>
    public void StoreLastSelectedUnit(Unit caster = null)
    {
        if (IsValidSelection() && CurrentSelection is Unit unit)
        {
            // Determine if this is an ally or enemy relative to the caster
            if (caster != null)
            {
                if (unit.IsPlayerUnit == caster.IsPlayerUnit)
                {
                    lastSelectedAlly = unit;
                }
                else
                {
                    lastSelectedEnemy = unit;
                }
            }
            else
            {
                // If no caster, store based on unit type (player = ally, enemy = enemy)
                if (unit.IsPlayerUnit)
                {
                    lastSelectedAlly = unit;
                }
                else
                {
                    lastSelectedEnemy = unit;
                }
            }
        }
    }

    /// <summary>
    /// Gets valid units based on target type and optional skill requirements
    /// Units are sorted by spawn index for consistent ordering
    /// </summary>
    private List<Unit> GetValidUnits(UnitTargetType targetType, Unit caster = null, Skill skill = null, Item item = null)
    {
        List<Unit> validUnits = new List<Unit>();
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // Check if we're using a healing skill or item
        bool isHealing = false;
        if (skill != null && skill.effectType == SkillEffectType.Heal)
        {
            isHealing = true;
        }
        else if (item != null && item.itemType == ItemType.Consumable && item.consumableSubtype == ConsumableSubtype.Heal)
        {
            isHealing = true;
        }

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
            
            // If an item is provided, validate against item's targeting requirements
            if (isValid && item != null && caster != null)
            {
                bool isItemValidTarget = false;
                switch (item.targetType)
                {
                    case SkillTargetType.Self:
                        isItemValidTarget = unit == caster;
                        break;
                    case SkillTargetType.Ally:
                        isItemValidTarget = caster.IsPlayerUnit == unit.IsPlayerUnit;
                        break;
                    case SkillTargetType.Enemy:
                        isItemValidTarget = caster.IsPlayerUnit != unit.IsPlayerUnit;
                        break;
                    case SkillTargetType.Any:
                        isItemValidTarget = true;
                        break;
                }
                isValid = isItemValidTarget;
            }

            // If using a healing skill/item and targeting an ally, exclude full HP allies
            if (isValid && isHealing && caster != null)
            {
                // Check if this unit is an ally (for healing purposes)
                bool isAlly = caster.IsPlayerUnit == unit.IsPlayerUnit;
                
                if (isAlly && unit.CurrentHP >= unit.MaxHP)
                {
                    // Skip this unit - it's at full HP and doesn't need healing
                    isValid = false;
                }
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
        currentIndex = 0;

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
        
        // Clear all simple selection markers
        ClearAllSimpleSelectionMarkers();
        
        NotifySelectionChanged();
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Moves selection forward (next item)
    /// </summary>
    public void Next()
    {
        if (navigationDisabled || selectableItems.Count == 0)
            return;

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
        if (navigationDisabled || selectableItems.Count == 0)
            return;

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
        if (navigationDisabled || selectableItems.Count == 0)
        {
            return;
        }

        if (index < 0)
        {
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
        
        // Update simple selection markers
        UpdateSimpleSelectionMarkers();
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
                currentIndex = 0;
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

    #region Mouse Hover Tracking

    /// <summary>
    /// Gets the spawn index (0-5) for a unit (used for sorting)
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
    /// Updates mouse hover tracking based on which unit the mouse is over
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
        
        // Update hovered unit tracking
        hoveredUnit = newHoveredUnit;
    }

    /// <summary>
    /// Gets the unit that the mouse is currently hovering over
    /// </summary>
    public Unit GetUnitUnderMouse()
    {
        if (raycastCamera == null)
            return null;
        
        // Convert mouse position to world position
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = raycastCamera.nearClipPlane + 1f; // Set appropriate distance
        Vector3 worldPos = raycastCamera.ScreenToWorldPoint(mousePos);
        
        // Try 2D physics raycast first (for sprites with 2D colliders)
        Vector3 mouseWorldPos2D = raycastCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, raycastCamera.nearClipPlane));
        Collider2D hit2D = Physics2D.OverlapPoint(new Vector2(mouseWorldPos2D.x, mouseWorldPos2D.y));
        if (hit2D != null)
        {
            Unit unit = hit2D.GetComponent<Unit>();
            if (unit != null && unit.IsAlive())
            {
                return unit;
            }
        }
        
        // Try 3D physics raycast (if units have 3D colliders)
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
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
    /// Clears the hover tracking
    /// </summary>
    private void ClearHoverHighlight()
    {
        hoveredUnit = null;
        selectedUnit = null;
    }
    
    /// <summary>
    /// Handles mouse click to select a unit directly
    /// </summary>
    private void HandleMouseClickSelection()
    {
        // Don't process clicks if clicking on UI elements
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        
        // Get the unit under the mouse cursor
        Unit clickedUnit = GetUnitUnderMouse();
        
        if (clickedUnit != null && IsUnitSelectable(clickedUnit))
        {
            // Select the clicked unit
            SelectItem(clickedUnit);
        }
    }

    #endregion

    #region UI Selection Markers

    /// <summary>
    /// Shows or hides the UI selection marker at the specified index (0 or 1)
    /// Deprecated: This method is kept for compatibility but does nothing.
    /// Use SimpleSelectionMarker instead.
    /// </summary>
    /// <param name="markerIndex">Index of the marker (0 for SelectMarker1, 1 for SelectMarker2)</param>
    /// <param name="active">Whether to show (true) or hide (false) the marker</param>
    public void SetUISelectionMarker(int markerIndex, bool active)
    {
        // Deprecated: SelectMarker1 and SelectMarker2 have been removed.
        // Use SimpleSelectionMarker instead, which is automatically managed.
    }

    /// <summary>
    /// Hides all UI selection markers
    /// Deprecated: This method is kept for compatibility but does nothing.
    /// Use SimpleSelectionMarker instead.
    /// </summary>
    public void HideAllUISelectionMarkers()
    {
        // Deprecated: SelectMarker1 and SelectMarker2 have been removed.
        // Use SimpleSelectionMarker instead, which is automatically managed.
    }

    #endregion
    
    #region Simple Selection Markers
    
    /// <summary>
    /// Updates simple selection markers - only shows marker for currently selected item
    /// </summary>
    private void UpdateSimpleSelectionMarkers()
    {
        if (SimpleSelectionMarker == null)
        {
            Debug.LogWarning("Selection: SimpleSelectionMarker is null! Cannot update markers.");
            return;
        }
        
        // Get the currently selected item
        object selectedItem = IsValidSelection() ? selectableItems[currentIndex] : null;
        
        // Clear markers for items that are no longer selectable or not currently selected
        List<object> itemsToRemove = new List<object>();
        foreach (var kvp in selectionMarkers)
        {
            if (!selectableItems.Contains(kvp.Key) || kvp.Key != selectedItem)
            {
                itemsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var item in itemsToRemove)
        {
            DestroySimpleSelectionMarker(item);
        }
        
        // Create/update marker only for the currently selected item
        if (selectedItem != null)
        {
            // Check if selected item is a Button and if it's active in hierarchy
            if (selectedItem is Button button)
            {
                if (!button.gameObject.activeInHierarchy)
                {
                    // Button is in an inactive panel, don't show marker
                    if (selectionMarkers.ContainsKey(selectedItem))
                    {
                        GameObject marker = selectionMarkers[selectedItem];
                        if (marker != null)
                        {
                            marker.SetActive(false);
                        }
                    }
                    return;
                }
            }
            
            if (!selectionMarkers.ContainsKey(selectedItem))
            {
                CreateSimpleSelectionMarker(selectedItem);
            }
            
            UpdateSimpleSelectionMarker(selectedItem);
        }
    }
    
    /// <summary>
    /// Creates a simple selection marker for a selectable item
    /// </summary>
    private void CreateSimpleSelectionMarker(object item)
    {
        if (SimpleSelectionMarker == null || item == null)
            return;
        
        // Determine parent: use custom parent if set, otherwise use canvas
        Transform parentTransform = null;
        
        if (customMarkerParent != null)
        {
            parentTransform = customMarkerParent;
        }
        else
        {
            Canvas canvas = markerCanvas;
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogWarning("Selection: No Canvas found for SimpleSelectionMarker. Cannot create marker.");
                    return;
                }
            }
            parentTransform = canvas.transform;
        }
        
        // Instantiate marker
        GameObject marker = Instantiate(SimpleSelectionMarker, parentTransform);
        marker.SetActive(true);
        
        ApplyMarkerRenderOrder(marker.transform);
        
        selectionMarkers[item] = marker;
        
        // Disable raycast targeting on all Graphic components (Image, Text, RawImage, etc.)
        // so the marker doesn't block button clicks underneath
        // Also apply color and alpha to all Graphic components
        Graphic[] graphics = marker.GetComponentsInChildren<Graphic>(true);
        Color finalColor = new Color(markerColor.r, markerColor.g, markerColor.b, markerAlpha);
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
            graphic.color = finalColor;
        }
        
        // Ensure it has a RectTransform
        RectTransform rectTransform = marker.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = marker.AddComponent<RectTransform>();
        }
        
        // Set initial position and scale
        UpdateSimpleSelectionMarker(item);
    }
    
    /// <summary>
    /// Updates the position and scale of a simple selection marker to cover the selected object
    /// </summary>
    private void UpdateSimpleSelectionMarker(object item)
    {
        if (item == null || !selectionMarkers.ContainsKey(item))
            return;
        
        GameObject marker = selectionMarkers[item];
        if (marker == null)
        {
            selectionMarkers.Remove(item);
            return;
        }
        
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        if (markerRect == null)
            return;
        
        // Handle different object types
        if (item is Unit unit)
        {
            UpdateMarkerForUnit(markerRect, unit);
        }
        else if (item is Button button)
        {
            // Handle Button components specifically (most common case for selection)
            UpdateMarkerForButton(markerRect, button);
        }
        else if (item is GameObject gameObj)
        {
            UpdateMarkerForGameObject(markerRect, gameObj);
        }
        else if (item is Component component)
        {
            UpdateMarkerForGameObject(markerRect, component.gameObject);
        }
        else
        {
            // For other types, try to find a GameObject or Component
            // This is a fallback for skills, items, etc.
            UpdateMarkerForGenericObject(markerRect, item);
        }
    }
    
    /// <summary>
    /// Updates marker position and scale for a Unit
    /// </summary>
    private void UpdateMarkerForUnit(RectTransform markerRect, Unit unit)
    {
        if (unit == null || unit.transform == null)
            return;
        
        // Get camera for world to screen conversion
        Camera cam = raycastCamera;
        if (cam == null)
            cam = Camera.main;
        
        if (cam == null)
            return;
        
        // Get unit bounds (try to get renderer bounds, or use a default size)
        Bounds bounds = GetUnitBounds(unit);
        
        // Convert world bounds to screen space - use all 8 corners of the bounding box
        Vector3[] worldCorners = new Vector3[]
        {
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z)
        };
        
        Vector2 minScreen = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxScreen = new Vector2(float.MinValue, float.MinValue);
        
        foreach (var corner in worldCorners)
        {
            Vector3 screenPoint = cam.WorldToScreenPoint(corner);
            minScreen.x = Mathf.Min(minScreen.x, screenPoint.x);
            minScreen.y = Mathf.Min(minScreen.y, screenPoint.y);
            maxScreen.x = Mathf.Max(maxScreen.x, screenPoint.x);
            maxScreen.y = Mathf.Max(maxScreen.y, screenPoint.y);
        }
        
        // Convert screen space to canvas space
        Canvas canvas = markerRect.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                Vector2 minCanvas, maxCanvas;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, minScreen, canvas.worldCamera, out minCanvas);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, maxScreen, canvas.worldCamera, out maxCanvas);
                
                // Set marker position and size
                markerRect.anchoredPosition = (minCanvas + maxCanvas) / 2f;
                markerRect.sizeDelta = maxCanvas - minCanvas;
            }
        }
    }
    
    /// <summary>
    /// Updates marker position and scale for a Button (UI element)
    /// </summary>
    private void UpdateMarkerForButton(RectTransform markerRect, Button button)
    {
        if (button == null || button.transform == null)
            return;
        
        // Check if button is active in hierarchy (if panel is hidden, button won't be active)
        if (!button.gameObject.activeInHierarchy)
        {
            // Hide marker if button is not active
            if (markerRect != null)
            {
                markerRect.gameObject.SetActive(false);
            }
            return;
        }
        
        // Show marker if button is active
        if (markerRect != null)
        {
            markerRect.gameObject.SetActive(true);
        }
        
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect == null)
        {
            // Fallback to GameObject method if no RectTransform
            UpdateMarkerForGameObject(markerRect, button.gameObject);
            return;
        }
        
        // Get the canvas that contains the marker
        Canvas markerCanvas = markerRect.GetComponentInParent<Canvas>();
        Canvas buttonCanvas = buttonRect.GetComponentInParent<Canvas>();
        
        if (markerCanvas == null || buttonCanvas == null)
        {
            // Fallback if canvas not found
            UpdateMarkerForGameObject(markerRect, button.gameObject);
            return;
        }
        
        // Get world corners of the button
        Vector3[] buttonWorldCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonWorldCorners);
        
        // Convert button's world corners to canvas local space
        RectTransform canvasRect = markerCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            UpdateMarkerForGameObject(markerRect, button.gameObject);
            return;
        }
        
        // Convert world corners to canvas local points
        Vector2 minLocal = Vector2.zero;
        Vector2 maxLocal = Vector2.zero;
        
        bool firstPoint = true;
        foreach (Vector3 worldCorner in buttonWorldCorners)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                RectTransformUtility.WorldToScreenPoint(buttonCanvas.worldCamera ?? Camera.main, worldCorner),
                markerCanvas.worldCamera ?? Camera.main,
                out localPoint);
            
            if (firstPoint)
            {
                minLocal = localPoint;
                maxLocal = localPoint;
                firstPoint = false;
            }
            else
            {
                minLocal.x = Mathf.Min(minLocal.x, localPoint.x);
                minLocal.y = Mathf.Min(minLocal.y, localPoint.y);
                maxLocal.x = Mathf.Max(maxLocal.x, localPoint.x);
                maxLocal.y = Mathf.Max(maxLocal.y, localPoint.y);
            }
        }
        
        // Set marker to cover the button's area
        markerRect.anchoredPosition = (minLocal + maxLocal) / 2f;
        markerRect.sizeDelta = maxLocal - minLocal;
        
        // Set anchors to stretch mode for easier positioning
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
    }
    
    /// <summary>
    /// Updates marker position and scale for a GameObject
    /// </summary>
    private void UpdateMarkerForGameObject(RectTransform markerRect, GameObject gameObj)
    {
        if (gameObj == null)
            return;
        
        // Check if it's a UI element (has RectTransform)
        RectTransform targetRect = gameObj.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            // It's a UI element - copy its rect
            markerRect.position = targetRect.position;
            markerRect.sizeDelta = targetRect.sizeDelta;
            markerRect.anchorMin = targetRect.anchorMin;
            markerRect.anchorMax = targetRect.anchorMax;
            markerRect.pivot = targetRect.pivot;
        }
        else
        {
            // It's a world-space object - convert to screen space
            Camera cam = raycastCamera;
            if (cam == null)
                cam = Camera.main;
            
            if (cam == null)
                return;
            
            // Get bounds
            Bounds bounds = GetGameObjectBounds(gameObj);
            
            // Convert to screen space (similar to unit method) - use all 8 corners of the bounding box
            Vector3[] worldCorners = new Vector3[]
            {
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z)
            };
            
            Vector2 minScreen = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 maxScreen = new Vector2(float.MinValue, float.MinValue);
            
            foreach (var corner in worldCorners)
            {
                Vector3 screenPoint = cam.WorldToScreenPoint(corner);
                minScreen.x = Mathf.Min(minScreen.x, screenPoint.x);
                minScreen.y = Mathf.Min(minScreen.y, screenPoint.y);
                maxScreen.x = Mathf.Max(maxScreen.x, screenPoint.x);
                maxScreen.y = Mathf.Max(maxScreen.y, screenPoint.y);
            }
            
            // Convert to canvas space
            Canvas canvas = markerRect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    Vector2 minCanvas, maxCanvas;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, minScreen, canvas.worldCamera, out minCanvas);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, maxScreen, canvas.worldCamera, out maxCanvas);
                    
                    markerRect.anchoredPosition = (minCanvas + maxCanvas) / 2f;
                    markerRect.sizeDelta = maxCanvas - minCanvas;
                }
            }
        }
    }
    
    /// <summary>
    /// Updates marker for generic objects (skills, items, etc.)
    /// </summary>
    private void UpdateMarkerForGenericObject(RectTransform markerRect, object item)
    {
        // For generic objects, try to find associated UI elements or use a default size
        // This is a fallback - you may want to customize this based on your needs
        
        // Try to find if there's a UI button or element associated with this item
        // For now, we'll just hide the marker if we can't determine its position
        markerRect.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Gets the bounds of a Unit for marker sizing
    /// </summary>
    private Bounds GetUnitBounds(Unit unit)
    {
        if (unit == null || unit.transform == null)
            return new Bounds(Vector3.zero, Vector3.one);
        
        // Try to get renderer bounds
        Renderer renderer = unit.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // Try sprite renderer
        SpriteRenderer spriteRenderer = unit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds;
        }
        
        // Fallback: use a default size based on transform
        return new Bounds(unit.transform.position, Vector3.one * 2f);
    }
    
    /// <summary>
    /// Gets the bounds of a GameObject for marker sizing
    /// </summary>
    private Bounds GetGameObjectBounds(GameObject gameObj)
    {
        if (gameObj == null)
            return new Bounds(Vector3.zero, Vector3.one);
        
        // Try to get renderer bounds
        Renderer renderer = gameObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        // Try sprite renderer
        SpriteRenderer spriteRenderer = gameObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds;
        }
        
        // Fallback: use a default size based on transform
        return new Bounds(gameObj.transform.position, Vector3.one * 2f);
    }
    
    /// <summary>
    /// Destroys a simple selection marker for an item
    /// </summary>
    private void DestroySimpleSelectionMarker(object item)
    {
        if (item != null && selectionMarkers.ContainsKey(item))
        {
            GameObject marker = selectionMarkers[item];
            if (marker != null)
            {
                Destroy(marker);
            }
            selectionMarkers.Remove(item);
        }
    }
    
    /// <summary>
    /// Clears all simple selection markers
    /// </summary>
    private void ClearAllSimpleSelectionMarkers()
    {
        foreach (var kvp in selectionMarkers)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        selectionMarkers.Clear();
    }
    
    /// <summary>
    /// Sets whether markers should render in front of other UI (true) or behind (false)
    /// </summary>
    public void SetMarkersRenderInFront(bool inFront)
    {
        markersRenderInFront = inFront;
        
        foreach (var kvp in selectionMarkers)
        {
            if (kvp.Value != null)
            {
                ApplyMarkerRenderOrder(kvp.Value.transform);
            }
        }
    }
    
    /// <summary>
    /// Sets a custom parent GameObject for selection markers (null to use default canvas)
    /// If markerParent is set in inspector, uses that; otherwise uses the provided parent parameter
    /// </summary>
    public void SetMarkerParent(Transform parent)
    {
        // Use markerParent from inspector if set, otherwise use the provided parent parameter
        Transform targetParent = null;
        if (markerParent != null)
        {
            targetParent = markerParent.transform;
        }
        else if (parent != null)
        {
            targetParent = parent;
        }
        
        customMarkerParent = targetParent;
        
        // Reparent existing markers if any
        foreach (var kvp in selectionMarkers)
        {
            if (kvp.Value != null)
            {
                Transform finalParent = targetParent;
                if (finalParent == null && markerCanvas != null)
                {
                    finalParent = markerCanvas.transform;
                }
                if (finalParent != null)
                {
                    kvp.Value.transform.SetParent(finalParent);
                    ApplyMarkerRenderOrder(kvp.Value.transform);
                }
            }
        }
    }
    
    /// <summary>
    /// Applies the current render order preference to a specific marker
    /// </summary>
    private void ApplyMarkerRenderOrder(Transform markerTransform)
    {
        if (markerTransform == null || markerTransform.parent == null)
            return;
        
        if (markersRenderInFront)
        {
            markerTransform.SetAsLastSibling();
        }
        else
        {
            markerTransform.SetAsFirstSibling();
        }
    }
    
    /// <summary>
    /// Enables or disables navigation (prevents Next/Previous/SetIndex from changing selection)
    /// The marker will still be visible, but won't move when navigation is disabled
    /// </summary>
    public void SetNavigationEnabled(bool enabled)
    {
        navigationDisabled = !enabled;
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
