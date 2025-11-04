using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private TextMeshProUGUI[] skillNames;
    private Image[] skillIcons;
    private Button[] skillButtons;
    
    // Selection mode state
    private bool isSelectionMode = false;
    private int selectedSkillIndex = -1; // The skill index that triggered selection mode
    private Unit currentCastingUnit = null; // The unit casting the skill
    private Skill currentSkill = null; // The skill being cast

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

    private void Update()
    {
        // Check for cancellation (right click or ESC)
        if (isSelectionMode)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                CancelSelectionMode();
            }
            // Check for mouse click on a unit (direct selection)
            else if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClickOnUnit();
            }
            // Navigate through targets with arrow keys or WASD
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (selection != null)
                {
                    selection.Previous();
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (selection != null)
                {
                    selection.Next();
                }
            }
            // Confirm selection with Enter or Space (keyboard navigation confirmation)
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmSelection();
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
            return;
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
            }
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
                // Advance turn after delays complete
                StartCoroutine(DelayedAdvanceTurnAfterSkill());
            }
            else
            {
                // Fallback to immediate execution
                currentUnit.UseSkill(skillIndex, currentUnit);
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

        isSelectionMode = true;
        selectedSkillIndex = skillIndex;
        currentCastingUnit = caster;
        currentSkill = skill;

        // Convert SkillTargetType to UnitTargetType
        UnitTargetType targetType = ConvertTargetType(skill.targetType);

        // Setup unit selection
        selection.SetupUnitSelection(targetType, caster, skill);

        Debug.Log($"Entered selection mode for skill: {skill.skillName}, Target type: {targetType}");

        // If no valid targets, exit selection mode
        if (selection.Count == 0)
        {
            Debug.LogWarning("No valid targets for this skill!");
            CancelSelectionMode();
        }
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
                return UnitTargetType.Allies;
            case SkillTargetType.Enemy:
                return UnitTargetType.Enemies;
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

        // Get the unit under the mouse cursor
        Unit clickedUnit = GetUnitUnderMouse();
        
        if (clickedUnit != null)
        {
            // Check if the clicked unit is selectable
            if (IsUnitSelectable(clickedUnit))
            {
                // Select and immediately confirm this unit
                selection.SelectItem(clickedUnit);
                ConfirmSelection();
                return;
            }
        }
        
        // If no unit was clicked, do nothing (don't confirm keyboard selection on random clicks)
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
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
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
                Debug.Log($"[SkillPanel] Calling ExecuteSkillWithDelay on GameManager");
                gameManager.ExecuteSkillWithDelay(caster, skillIndex, target, false);
                // Advance turn after delays complete
                StartCoroutine(DelayedAdvanceTurnAfterSkill());
            }
            else
            {
                Debug.LogWarning($"[SkillPanel] GameManager is null! Using fallback.");
                // Fallback to immediate execution
                caster.UseSkill(skillIndex, target);
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
            
        // Wait for skill animation + hit animation delays
        float totalDelay = gameManager.skillAnimationDelay + gameManager.hitAnimationDelay;
        yield return new WaitForSeconds(totalDelay);
        
        // Advance turn
        AdvanceTurn();
    }

    /// <summary>
    /// Cancels selection mode
    /// </summary>
    private void CancelSelectionMode()
    {
        if (!isSelectionMode)
            return;

        isSelectionMode = false;
        selectedSkillIndex = -1;
        currentCastingUnit = null;
        currentSkill = null;

        if (selection != null)
        {
            selection.ClearSelection();
        }

        Debug.Log("Selection mode cancelled");
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
