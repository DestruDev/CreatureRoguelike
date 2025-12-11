using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SkillPanelManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Selection selection; // Reference to Selection component

    [Header("Skill Names")]
    public TextMeshProUGUI skill1Name;
    public TextMeshProUGUI skill2Name;
    public TextMeshProUGUI skill3Name;
    public TextMeshProUGUI skill4Name;

    [Header("Skill Icons")]
    public Image skill1Icon;
    public Image skill2Icon;
    public Image skill3Icon;
    public Image skill4Icon;

    [Header("Skill Buttons")]
    public Button skill1Button;
    public Button skill2Button;
    public Button skill3Button;
    public Button skill4Button;

    [Header("Cooldown Text")]
    public TextMeshProUGUI skill1Cooldown;
    public TextMeshProUGUI skill2Cooldown;
    public TextMeshProUGUI skill3Cooldown;
    public TextMeshProUGUI skill4Cooldown;

    [Header("Cooldown Settings")]
    [Range(0f, 1f)]
    [Tooltip("Brightness value (V) for skills on cooldown. 0 = black, 1 = full brightness")]
    public float cooldownBrightness = 0.5f;

    private TextMeshProUGUI[] skillNames;
    private Image[] skillIcons;
    private Button[] skillButtons;
    private TextMeshProUGUI[] skillCooldowns;
    
    // Store original colors for restoration
    private Color[] originalIconColors = new Color[4];
    private Color[] originalButtonColors = new Color[4];
    private bool[] originalColorsStored = new bool[4];
    
    // Track current unit to reset colors when switching units
    private Unit lastDisplayedUnit = null;
    
    // Selection mode state
    private bool isSelectionMode = false;
    private int selectedSkillIndex = -1; // The skill index that triggered selection mode
    private Unit currentCastingUnit = null; // The unit casting the skill
    private Skill currentSkill = null; // The skill being cast
    
    // Button selection mode state (for navigating skill buttons)
    private bool isButtonSelectionMode = false;
    private bool ignoreInputThisFrame = false; // Prevents accidental activation when opening panel
    private int lastSelectedButtonIndex = -1; // Track which button was selected before entering selection mode

    private void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Find Selection component if not assigned
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
        }

        // Initialize arrays for easier iteration
        skillNames = new TextMeshProUGUI[] { skill1Name, skill2Name, skill3Name, skill4Name };
        skillIcons = new Image[] { skill1Icon, skill2Icon, skill3Icon, skill4Icon };
        skillButtons = new Button[] { skill1Button, skill2Button, skill3Button, skill4Button };
        skillCooldowns = new TextMeshProUGUI[] { skill1Cooldown, skill2Cooldown, skill3Cooldown, skill4Cooldown };

        // Subscribe to button clicks
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int skillIndex = i; // Capture index for closure
            if (skillButtons[i] != null)
            {
                skillButtons[i].onClick.AddListener(() => OnSkillButtonClicked(skillIndex));
            }
        }

        // Subscribe to selection events
        if (selection != null)
        {
            selection.OnSelectionChanged += OnSelectionChanged;
        }

        // Update skills on start
        UpdateSkills();
    }
    
    /// <summary>
    /// Called when the Skills panel becomes visible - enables button selection mode
    /// </summary>
    private void OnEnable()
    {
        EnableButtonSelectionMode();
    }
    
    /// <summary>
    /// Called when the Skills panel becomes hidden - disables button selection mode
    /// </summary>
    private void OnDisable()
    {
        DisableButtonSelectionMode();
    }
    
    /// <summary>
    /// Enables button selection mode for navigating skill buttons vertically
    /// </summary>
    public void EnableButtonSelectionMode()
    {
        if (selection == null)
        {
            selection = FindFirstObjectByType<Selection>();
            if (selection == null)
            {
                Debug.LogWarning("SkillPanelManager: Selection component not found. Button selection mode disabled.");
                return;
            }
        }
        
        // Clear any existing selection first to ensure we start fresh
        selection.ClearSelection();
        
        isButtonSelectionMode = true;
        
        // Get available skill buttons (only buttons with skills)
        List<Button> availableButtons = new List<Button>();
        if (gameManager != null)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            if (currentUnit != null && currentUnit.Skills != null)
            {
                for (int i = 0; i < 4 && i < currentUnit.Skills.Length; i++)
                {
                    if (currentUnit.Skills[i] != null && skillButtons[i] != null)
                    {
                        availableButtons.Add(skillButtons[i]);
                    }
                }
            }
        }
        
        // Set up selection with available buttons
        if (availableButtons.Count > 0)
        {
            selection.SetSelection(availableButtons.ToArray(), SelectionType.UIButtons);
            // Ignore input for this frame to prevent accidental activation
            ignoreInputThisFrame = true;
        }
    }
    
    /// <summary>
    /// Disables button selection mode
    /// </summary>
    private void DisableButtonSelectionMode()
    {
        isButtonSelectionMode = false;
        
        if (selection != null)
        {
            selection.ClearSelection();
        }
    }
    
    /// <summary>
    /// Handles input for button selection mode (Up/Down or W/S to cycle, Enter/Space to activate)
    /// </summary>
    private void HandleButtonSelectionInput()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        // Cycle with Up/Down arrow keys or W/S (W = previous, S = next)
        if (Keyboard.current != null && (Keyboard.current[Key.UpArrow].wasPressedThisFrame || Keyboard.current[Key.W].wasPressedThisFrame))
        {
            selection.Previous();
        }
        else if (Keyboard.current != null && (Keyboard.current[Key.DownArrow].wasPressedThisFrame || Keyboard.current[Key.S].wasPressedThisFrame))
        {
            selection.Next();
        }
        
        // Activate selected button with Enter or Space
        if (Keyboard.current != null && (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame))
        {
            // Block input during skill execution
            ActionPanelManager actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
            if (actionPanelManager != null && actionPanelManager.IsSkillExecuting())
            {
                return;
            }
            
            ActivateSelectedButton();
        }
    }
    
    /// <summary>
    /// Activates the currently selected skill button
    /// </summary>
    private void ActivateSelectedButton()
    {
        if (selection == null || !selection.IsValidSelection())
            return;
        
        object selectedItem = selection.CurrentSelection;
        if (selectedItem is Button button)
        {
            // Find which skill button was selected
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == button)
                {
                    OnSkillButtonClicked(i);
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // Only process input if this GameObject is active in the hierarchy
        if (!gameObject.activeInHierarchy)
            return;
        
        // Check if SkillsPanel is actually visible (via ActionPanelManager)
        ActionPanelManager actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        if (actionPanelManager != null && actionPanelManager.SkillsPanel != null && !actionPanelManager.SkillsPanel.activeSelf)
        {
            // Skills panel is not visible, don't process input
            return;
        }
        
        // Update skills to reflect cooldown changes (only for player units, and only update brightness)
        if (gameManager != null && !isSelectionMode && !isButtonSelectionMode)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            if (currentUnit != null && currentUnit.IsPlayerUnit && currentUnit == lastDisplayedUnit)
            {
                // Only update brightness and cooldown text without full refresh (more efficient)
                Skill[] skills = currentUnit.Skills;
                for (int i = 0; i < 4 && i < skills.Length; i++)
                {
                    if (skills[i] != null && originalColorsStored[i])
                    {
                        bool isUnusable = !currentUnit.CanUseSkill(i);
                        UpdateSkillBrightness(i, isUnusable);
                        UpdateCooldownText(i, currentUnit);
                    }
                }
            }
        }
        
        // Handle button selection mode (vertical navigation through skill buttons)
        if (isButtonSelectionMode && !isSelectionMode)
        {
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
            }
            else
            {
                HandleButtonSelectionInput();
            }
        }
        
        // Check for cancellation (right click or ESC)
        if (isSelectionMode)
        {
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
            }
            else
            {
                if ((Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame) || 
                    (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame))
                {
                    CancelSelectionMode();
                }
                // Mouse click handling is done in LateUpdate() to ensure Selection class processes it first
                // Navigate through targets with arrow keys or WASD
                else if (Keyboard.current != null && (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || Keyboard.current[Key.A].wasPressedThisFrame))
                {
                    if (selection != null)
                    {
                        selection.Previous();
                    }
                }
                else if (Keyboard.current != null && (Keyboard.current[Key.RightArrow].wasPressedThisFrame || Keyboard.current[Key.D].wasPressedThisFrame))
                {
                    if (selection != null)
                    {
                        selection.Next();
                    }
                }
                // Confirm selection with Enter or Space (keyboard navigation confirmation)
                else if (Keyboard.current != null && (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame))
                {
                    // Block input during skill execution
                    if (actionPanelManager != null && actionPanelManager.IsSkillExecuting())
                    {
                        return;
                    }
                    
                    ConfirmSelection();
                }
            }
        }
    }
    
    void LateUpdate()
    {
        // Check for mouse click confirmation after Selection class has processed clicks
        // This ensures we check after Selection's Update has run
        if (isSelectionMode && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Don't process if clicking on UI elements
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            // Skip input handling if we're ignoring input this frame
            if (ignoreInputThisFrame)
            {
                ignoreInputThisFrame = false;
                return;
            }
            
            // The Selection class's Update() has already handled the mouse click and selected the unit
            // We just need to confirm if there's a valid selection (meaning a unit was clicked)
            if (selection != null && selection.IsValidSelection())
            {
                Unit selectedUnit = selection.GetSelectedUnit();
                // Only confirm if we have a valid unit selected (not empty space)
                if (selectedUnit != null)
                {
                    // Show marker briefly before confirming to give visual feedback
                    StartCoroutine(DelayedConfirmSelection(0.15f));
                }
            }
        }
    }

    /// <summary>
    /// Updates the skill UI to match the current unit's skills
    /// </summary>
    public void UpdateSkills()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("SkillPanelManager: GameManager not found!");
            return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();

        if (currentUnit == null)
        {
            // Clear all skill displays if no unit
            ClearAllSkills();
            lastDisplayedUnit = null;
            return;
        }

        // If unit changed, reset all stored colors and restore slots to original state
        if (currentUnit != lastDisplayedUnit)
        {
            ResetAllSkillColors();
            lastDisplayedUnit = currentUnit;
        }

        Skill[] skills = currentUnit.Skills;

        // Update each skill slot
        for (int i = 0; i < 4; i++)
        {
            if (i < skills.Length && skills[i] != null)
            {
                // Update skill name
                if (skillNames[i] != null)
                {
                    skillNames[i].text = skills[i].skillName;
                }

                // Store original colors if not already stored
                if (!originalColorsStored[i])
                {
                    // Store original icon color
                    if (skillIcons[i] != null)
                    {
                        originalIconColors[i] = skillIcons[i].color;
                    }
                    
                    // Store original button color
                    if (skillButtons[i] != null && skillButtons[i].image != null)
                    {
                        originalButtonColors[i] = skillButtons[i].image.color;
                    }
                    
                    originalColorsStored[i] = true;
                }
                
                // Update skill icon
                if (skillIcons[i] != null)
                {
                    if (skills[i].icon != null)
                    {
                        skillIcons[i].sprite = skills[i].icon;
                        skillIcons[i].enabled = true;
                    }
                    else
                    {
                        // If no icon, disable the image
                        skillIcons[i].enabled = false;
                    }
                }
                
                // Update brightness based on cooldown or unusability
                bool isUnusable = !currentUnit.CanUseSkill(i);
                UpdateSkillBrightness(i, isUnusable);
                
                // Update cooldown text
                UpdateCooldownText(i, currentUnit);
            }
            else
            {
                // Clear this skill slot
                if (skillNames[i] != null)
                {
                    skillNames[i].text = "";
                }

                if (skillIcons[i] != null)
                {
                    skillIcons[i].enabled = false;
                }
                
                // Clear cooldown text
                if (skillCooldowns[i] != null)
                {
                    skillCooldowns[i].text = "";
                }
                
                // Restore colors when clearing slot
                if (originalColorsStored[i])
                {
                    if (skillIcons[i] != null)
                    {
                        skillIcons[i].color = originalIconColors[i];
                    }
                    if (skillButtons[i] != null && skillButtons[i].image != null)
                    {
                        skillButtons[i].image.color = originalButtonColors[i];
                    }
                }
                // If not stored, leave it as is (don't modify alpha)
                
                // Reset stored flag
                originalColorsStored[i] = false;
            }
        }
        
        // Refresh button selection if in button selection mode (in case available buttons changed)
        if (isButtonSelectionMode)
        {
            EnableButtonSelectionMode();
        }
    }

    /// <summary>
    /// Resets all skill colors to their original state when switching units
    /// </summary>
    private void ResetAllSkillColors()
    {
        for (int i = 0; i < 4; i++)
        {
            // Restore icon color
            if (skillIcons[i] != null)
            {
                if (originalColorsStored[i])
                {
                    skillIcons[i].color = originalIconColors[i];
                }
                // If not stored, leave it as is (don't modify)
            }
            
            // Restore button color
            if (skillButtons[i] != null && skillButtons[i].image != null)
            {
                if (originalColorsStored[i])
                {
                    skillButtons[i].image.color = originalButtonColors[i];
                }
                // If not stored, leave it as is (don't modify)
            }
            
            // Reset stored flag so colors will be re-stored for new unit
            originalColorsStored[i] = false;
        }
    }

    /// <summary>
    /// Updates the brightness of a skill's icon and button based on cooldown status
    /// </summary>
    private void UpdateSkillBrightness(int skillIndex, bool isOnCooldown)
    {
        if (skillIndex < 0 || skillIndex >= 4)
            return;
        
        // Update icon brightness
        if (skillIcons[skillIndex] != null && skillIcons[skillIndex].enabled)
        {
            if (isOnCooldown)
            {
                // Apply brightness value to icon
                Color originalColor = originalIconColors[skillIndex];
                Color cooldownColor = new Color(
                    originalColor.r * cooldownBrightness,
                    originalColor.g * cooldownBrightness,
                    originalColor.b * cooldownBrightness,
                    originalColor.a
                );
                skillIcons[skillIndex].color = cooldownColor;
            }
            else
            {
                // Restore original color
                if (originalColorsStored[skillIndex])
                {
                    skillIcons[skillIndex].color = originalIconColors[skillIndex];
                }
            }
        }
        
        // Update button brightness
        if (skillButtons[skillIndex] != null && skillButtons[skillIndex].image != null)
        {
            if (isOnCooldown)
            {
                // Apply brightness value to button
                Color originalColor = originalButtonColors[skillIndex];
                Color cooldownColor = new Color(
                    originalColor.r * cooldownBrightness,
                    originalColor.g * cooldownBrightness,
                    originalColor.b * cooldownBrightness,
                    originalColor.a
                );
                skillButtons[skillIndex].image.color = cooldownColor;
            }
            else
            {
                // Restore original color
                if (originalColorsStored[skillIndex])
                {
                    skillButtons[skillIndex].image.color = originalButtonColors[skillIndex];
                }
            }
        }
    }

    /// <summary>
    /// Updates the cooldown text for a skill
    /// </summary>
    private void UpdateCooldownText(int skillIndex, Unit unit)
    {
        if (skillIndex < 0 || skillIndex >= 4 || skillCooldowns[skillIndex] == null || unit == null)
            return;

        int cooldown = unit.GetSkillCooldown(skillIndex);
        
        if (cooldown > 0)
        {
            // Show cooldown count
            skillCooldowns[skillIndex].text = cooldown.ToString();
        }
        else
        {
            // Clear text when skill is available
            skillCooldowns[skillIndex].text = "";
        }
    }

    /// <summary>
    /// Clears all skill displays
    /// </summary>
    private void ClearAllSkills()
    {
        for (int i = 0; i < 4; i++)
        {
            if (skillNames[i] != null)
            {
                skillNames[i].text = "";
            }

            if (skillIcons[i] != null)
            {
                skillIcons[i].enabled = false;
            }
            
            // Clear cooldown text
            if (skillCooldowns[i] != null)
            {
                skillCooldowns[i].text = "";
            }
            
            // Restore colors when clearing
            if (originalColorsStored[i])
            {
                if (skillIcons[i] != null)
                {
                    skillIcons[i].color = originalIconColors[i];
                }
                if (skillButtons[i] != null && skillButtons[i].image != null)
                {
                    skillButtons[i].image.color = originalButtonColors[i];
                }
            }
            // If not stored, leave it as is (don't modify alpha)
            
            originalColorsStored[i] = false;
        }
    }

    #region Skill Button Handlers

    /// <summary>
    /// Called when a skill button is clicked
    /// </summary>
    private void OnSkillButtonClicked(int skillIndex)
    {
        if (gameManager == null)
        {
            Debug.LogWarning("SkillPanelManager: GameManager not found!");
            return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();

        // Only allow skill selection during player unit's turn
        if (currentUnit == null || !currentUnit.IsPlayerUnit || isSelectionMode)
        {
            return;
        }
        
        // Don't allow skill usage during inspect mode
        InspectPanelManager inspectPanel = FindFirstObjectByType<InspectPanelManager>();
        if (inspectPanel != null && inspectPanel.IsInspectMode())
        {
            return;
        }

        // Check if unit has this skill
        if (skillIndex < 0 || skillIndex >= currentUnit.Skills.Length || currentUnit.Skills[skillIndex] == null)
        {
            return;
        }

        // Check if skill is available (not on cooldown)
        if (!currentUnit.CanUseSkill(skillIndex))
        {
            Debug.Log($"Skill {skillIndex} is on cooldown!");
            return;
        }

        Skill skill = currentUnit.Skills[skillIndex];

        // If skill targets self, use it with delay for proper animation timing
        if (skill.targetType == SkillTargetType.Self)
        {
            if (gameManager != null)
            {
                gameManager.ExecuteSkillWithDelay(currentUnit, skillIndex, currentUnit, false);
                // Update skills immediately to reflect cooldown
                UpdateSkills();
                // Advance turn after delays complete
                StartCoroutine(DelayedAdvanceTurnAfterSkill());
            }
            else
            {
                // Fallback to immediate execution
                currentUnit.UseSkill(skillIndex, currentUnit);
                UpdateSkills();
                AdvanceTurn();
            }
            return;
        }

        // Enter selection mode
        EnterSelectionMode(skillIndex, currentUnit, skill);
    }

    /// <summary>
    /// Enters selection mode for choosing a target
    /// </summary>
    private void EnterSelectionMode(int skillIndex, Unit caster, Skill skill)
    {
        if (selection == null)
        {
            Debug.LogWarning("SkillPanelManager: Selection component not found!");
            return;
        }

        // Store which button was selected before entering selection mode
        if (selection != null && selection.IsValidSelection())
        {
            object selectedItem = selection.CurrentSelection;
            if (selectedItem is Button button)
            {
                // Find which skill button was selected
                for (int i = 0; i < skillButtons.Length; i++)
                {
                    if (skillButtons[i] == button)
                    {
                        lastSelectedButtonIndex = i;
                        break;
                    }
                }
            }
        }

        // Disable button selection mode before entering target selection mode
        DisableButtonSelectionMode();

        isSelectionMode = true;
        selectedSkillIndex = skillIndex;
        currentCastingUnit = caster;
        currentSkill = skill;

        // Hide user panel when entering selection mode
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.HideUserPanel();
        }

        // Convert SkillTargetType to UnitTargetType
        UnitTargetType targetType = ConvertTargetType(skill.targetType);

        // Setup unit selection (will automatically restore previously selected unit based on target type)
        selection.SetupUnitSelection(targetType, caster, skill);

        Debug.Log($"Entered selection mode for skill: {skill.skillName}, Target type: {targetType}");

        // If no valid targets, exit selection mode
        if (selection.Count == 0)
        {
            Debug.LogWarning("No valid targets for this skill!");
            CancelSelectionMode();
            return;
        }
        
        // Ignore input for this frame to prevent accidental confirmation
        ignoreInputThisFrame = true;
    }

    /// <summary>
    /// Converts SkillTargetType to UnitTargetType
    /// </summary>
    private UnitTargetType ConvertTargetType(SkillTargetType skillTargetType)
    {
        switch (skillTargetType)
        {
            case SkillTargetType.Self:
                return UnitTargetType.Self;
            case SkillTargetType.Ally:
                return UnitTargetType.AllAllies; // Use AllAllies to include self (for heal abilities)
            case SkillTargetType.Enemy:
                return UnitTargetType.AllEnemies;
            case SkillTargetType.Any:
                return UnitTargetType.Any;
            default:
                return UnitTargetType.Any;
        }
    }

    /// <summary>
    /// Called when selection changes (for visual feedback, etc.)
    /// </summary>
    private void OnSelectionChanged(object selectedItem)
    {
        if (isSelectionMode && selectedItem is Unit selectedUnit)
        {
            // You can add visual feedback here (highlight selected unit, etc.)
            Debug.Log($"Selected unit: {selectedUnit.gameObject.name}");
        }
    }

    /// <summary>
    /// Handles mouse click to check if clicking directly on a unit
    /// </summary>
    private void HandleMouseClickOnUnit()
    {
        if (!isSelectionMode || selection == null)
            return;

        // Don't process if clicking on UI elements
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // The Selection class's Update() has already handled the mouse click and selected the unit
        // We just need to confirm if there's a valid selection (meaning a unit was clicked)
        if (selection.IsValidSelection())
        {
            Unit selectedUnit = selection.GetSelectedUnit();
            // Only confirm if we have a valid unit selected (not empty space)
            if (selectedUnit != null)
            {
                ConfirmSelection();
            }
        }
    }

    /// <summary>
    /// Gets the unit under the mouse cursor (reuses Selection's method if available, or uses raycast)
    /// </summary>
    private Unit GetUnitUnderMouse()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return null;

        // Raycast from camera through mouse position
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = cam.ScreenPointToRay(mousePos);
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
        Unit[] allUnits = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Unit closestUnit = null;
        float closestDistance = 2f; // Same as Selection's hoverDetectionDistance

        // Project mouse to ground plane
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
    /// Checks if a unit is selectable (in the current selection list)
    /// </summary>
    private bool IsUnitSelectable(Unit unit)
    {
        if (unit == null || selection == null)
            return false;

        // Check if the unit is in the selection's selectable items
        object[] allItems = selection.GetAllItems();
        foreach (var item in allItems)
        {
            if (item is Unit u && u == unit)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Coroutine to delay confirmation so selection marker is visible
    /// </summary>
    private System.Collections.IEnumerator DelayedConfirmSelection(float delay)
    {
        yield return new WaitForSeconds(delay);
        ConfirmSelection();
    }
    
    /// <summary>
    /// Confirms the current selection and uses the skill
    /// </summary>
    private void ConfirmSelection()
    {
        if (!isSelectionMode || selection == null)
            return;

        Unit selectedTarget = selection.GetSelectedUnit();
        
        if (selectedTarget != null && currentCastingUnit != null && currentSkill != null && selectedSkillIndex >= 0)
        {
            // Save references before clearing selection mode
            Unit caster = currentCastingUnit;
            int skillIndex = selectedSkillIndex;
            Unit target = selectedTarget;
            
            // Exit selection mode
            CancelSelectionMode();
            
            // Use the skill on the selected target with delay for proper animation timing
            Debug.Log($"[SkillPanel] ConfirmSelection: caster={caster.UnitName}, skillIndex={skillIndex}, target={target.UnitName}");
            if (gameManager != null)
            {
                gameManager.ExecuteSkillWithDelay(caster, skillIndex, target, false);
                // Update skills immediately to reflect cooldown
                UpdateSkills();
                // Advance turn after delays complete
                StartCoroutine(DelayedAdvanceTurnAfterSkill());
            }
            else
            {
                Debug.LogWarning($"[SkillPanel] GameManager is null! Using fallback.");
                // Fallback to immediate execution
                caster.UseSkill(skillIndex, target);
                UpdateSkills();
                AdvanceTurn();
            }
        }
    }
    
    /// <summary>
    /// Coroutine to advance turn after skill animation delays complete
    /// </summary>
    private System.Collections.IEnumerator DelayedAdvanceTurnAfterSkill()
    {
        if (gameManager == null)
            yield break;
            
        // Wait for skill animation + attack-to-hurt delay + hit animation delays
        // Get the actual delay (accounts for animation-based delay if enabled)
        Unit currentUnit = gameManager != null ? gameManager.GetCurrentUnit() : null;
        float attackDelay = GetAttackToHurtDelay(currentUnit, gameManager);
        float totalDelay = gameManager.skillAnimationDelay + attackDelay + gameManager.hitAnimationDelay;
        yield return new WaitForSeconds(totalDelay);
        
        // Advance turn
        AdvanceTurn();
    }

    /// <summary>
    /// Checks if we're currently in target selection mode
    /// </summary>
    public bool IsInSelectionMode()
    {
        return isSelectionMode;
    }
    
    /// <summary>
    /// Gets the current skill being cast during selection mode
    /// </summary>
    public Skill GetCurrentSkill()
    {
        return currentSkill;
    }
    
    /// <summary>
    /// Gets the current skill index being cast during selection mode
    /// </summary>
    public int GetSelectedSkillIndex()
    {
        return selectedSkillIndex;
    }
    
    /// <summary>
    /// Cancels selection mode (public method for external cancellation)
    /// </summary>
    public void CancelSelectionModePublic()
    {
        CancelSelectionMode();
    }
    
    /// <summary>
    /// Cancels selection mode
    /// </summary>
    private void CancelSelectionMode()
    {
        if (!isSelectionMode)
            return;

        // Store the currently selected unit before clearing (for next time, based on target type)
        if (selection != null && currentCastingUnit != null)
        {
            selection.StoreLastSelectedUnit(currentCastingUnit);
        }

        isSelectionMode = false;
        // Don't clear selectedSkillIndex and currentSkill yet - keep them until button selection is restored
        // This prevents InfoPanel from flickering to the first skill during the transition

        if (selection != null)
        {
            selection.ClearSelection();
        }

        // Show user panel again when exiting selection mode
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        if (gameManager != null)
        {
            gameManager.ShowUserPanel();
        }

        // Store the button index to restore before re-enabling button selection mode
        int buttonIndexToRestore = lastSelectedButtonIndex;
        
        // Re-enable button selection mode to return to skill button navigation
        EnableButtonSelectionMode();
        
        // Try to restore selection immediately to prevent flickering
        bool restoredImmediately = false;
        if (buttonIndexToRestore >= 0 && buttonIndexToRestore < skillButtons.Length && skillButtons[buttonIndexToRestore] != null)
        {
            // Check if the button has a skill (is available)
            if (gameManager != null)
            {
                Unit currentUnit = gameManager.GetCurrentUnit();
                if (currentUnit != null && currentUnit.Skills != null && 
                    buttonIndexToRestore < currentUnit.Skills.Length && 
                    currentUnit.Skills[buttonIndexToRestore] != null)
                {
                    // Button is available, restore selection to it immediately
                    if (selection != null && selection.IsValidSelection())
                    {
                        object[] allItems = selection.GetAllItems();
                        if (allItems != null)
                        {
                            for (int i = 0; i < allItems.Length; i++)
                            {
                                if (allItems[i] is Button button && button == skillButtons[buttonIndexToRestore])
                                {
                                    selection.SetIndex(i);
                                    restoredImmediately = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Now clear the skill info after button selection is restored
        // This allows InfoPanel to use the stored skill during the transition
        if (restoredImmediately)
        {
            // Button selection restored immediately, safe to clear now
            selectedSkillIndex = -1;
            currentSkill = null;
            currentCastingUnit = null;
            lastSelectedButtonIndex = -1;
        }
        else if (buttonIndexToRestore >= 0)
        {
            // Will be restored by coroutine, clear after it completes
            StartCoroutine(RestoreButtonSelectionAfterDelay());
        }
        else
        {
            // No button to restore, safe to clear immediately
            selectedSkillIndex = -1;
            currentSkill = null;
            currentCastingUnit = null;
            lastSelectedButtonIndex = -1;
        }

        Debug.Log("Selection mode cancelled");
    }
    
    /// <summary>
    /// Coroutine to restore button selection after EnableButtonSelectionMode has set up the selection
    /// </summary>
    private System.Collections.IEnumerator RestoreButtonSelectionAfterDelay()
    {
        // Wait a frame to ensure EnableButtonSelectionMode has fully set up the selection
        yield return null;
        
        // Only restore if we're still in button selection mode (not if something else changed)
        if (!isButtonSelectionMode || selection == null)
        {
            // Clear skill info and reset index even if we can't restore
            selectedSkillIndex = -1;
            currentSkill = null;
            currentCastingUnit = null;
            lastSelectedButtonIndex = -1;
            yield break;
        }
        
        // Restore selection to the previously selected button (if it's still available)
        int buttonIndexToRestore = lastSelectedButtonIndex;
        lastSelectedButtonIndex = -1; // Reset immediately to prevent issues
        
        if (buttonIndexToRestore >= 0 && buttonIndexToRestore < skillButtons.Length && skillButtons[buttonIndexToRestore] != null)
        {
            // Check if the button has a skill (is available)
            if (gameManager != null)
            {
                Unit currentUnit = gameManager.GetCurrentUnit();
                if (currentUnit != null && currentUnit.Skills != null && 
                    buttonIndexToRestore < currentUnit.Skills.Length && 
                    currentUnit.Skills[buttonIndexToRestore] != null)
                {
                    // Button is available, restore selection to it
                    if (selection.IsValidSelection())
                    {
                        object[] allItems = selection.GetAllItems();
                        if (allItems != null)
                        {
                            for (int i = 0; i < allItems.Length; i++)
                            {
                                if (allItems[i] is Button button && button == skillButtons[buttonIndexToRestore])
                                {
                                    selection.SetIndex(i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Clear skill info after button selection is restored
        selectedSkillIndex = -1;
        currentSkill = null;
        currentCastingUnit = null;
    }

    /// <summary>
    /// Gets the appropriate attack-to-hurt delay based on caster's animation length
    /// </summary>
    private float GetAttackToHurtDelay(Unit caster, GameManager gameManager)
    {
        if (gameManager == null)
        {
            return 0.5f; // Fallback
        }
        
        if (caster != null)
        {
            UnitAnimations unitAnimations = caster.GetComponent<UnitAnimations>();
            if (unitAnimations != null)
            {
                float animationLength = unitAnimations.GetAttackAnimationLength();
                if (animationLength > 0f)
                {
                    // Return animation length + post-animation delay
                    return animationLength + gameManager.postAttackAnimationDelay;
                }
            }
        }
        // Fallback if animation length can't be determined
        return 0.5f;
    }
    
    /// <summary>
    /// Advances to the next turn
    /// </summary>
    private void AdvanceTurn()
    {
        if (gameManager == null)
            return;

        TurnOrder turnOrder = FindFirstObjectByType<TurnOrder>();
        if (turnOrder != null)
        {
            turnOrder.AdvanceToNextTurn();
        }
    }

    #endregion
}
