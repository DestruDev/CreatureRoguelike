using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class HoverTriggerObject
{
    public GameObject triggerObject;
    public Vector2 positionOffset = new Vector2(100, -90);
}

public class Hover : MonoBehaviour
{
    [SerializeField] private GameObject HoverUI;
    [SerializeField] private TextMeshProUGUI hoverText;
    [SerializeField] private List<HoverTriggerObject> hoverTriggerObjects = new List<HoverTriggerObject>();
    
    [Header("Hover Settings")]
    [SerializeField] private bool enableSkillHovers = true;
    [SerializeField] private bool enableItemHovers = true;
    
    private Camera mainCamera;
    private bool isHovering = false;
    private RectTransform hoverUIRectTransform;
    private Canvas canvas;
    private Vector2 currentOffset = Vector2.zero;
    private Skill currentSkill = null;
    private Item currentItem = null;
    
    // References to panel managers
    private SkillPanelManager skillPanelManager;
    private ItemPanelManager itemPanelManager;
    private GameManager gameManager;
    private Inventory inventory;
    
#if UNITY_EDITOR
    void OnValidate()
    {
        // Ensure default position offset is applied in the Editor when items are added
        foreach (var triggerObj in hoverTriggerObjects)
        {
            if (triggerObj != null && triggerObj.positionOffset == Vector2.zero)
            {
                triggerObj.positionOffset = new Vector2(100, -90);
            }
        }
    }
#endif
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Ensure default position offset is applied to any items that don't have it set
        foreach (var triggerObj in hoverTriggerObjects)
        {
            if (triggerObj != null && triggerObj.positionOffset == Vector2.zero)
            {
                triggerObj.positionOffset = new Vector2(100, -90);
            }
        }
        
        // Hide the UI GameObject by default
        if (HoverUI != null)
        {
            HoverUI.SetActive(false);
            hoverUIRectTransform = HoverUI.GetComponent<RectTransform>();
            
            // Find the canvas that contains the HoverUI
            canvas = HoverUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
        }
        
        // Get the main camera for raycasting
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }
        
        // Find panel managers
        skillPanelManager = FindFirstObjectByType<SkillPanelManager>();
        itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        inventory = FindFirstObjectByType<Inventory>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckHover();
        
        // Update UI position to follow mouse when hovering
        if (isHovering && hoverUIRectTransform != null)
        {
            UpdateUIPosition();
        }
    }
    
    void CheckHover()
    {
        bool currentlyHovering = false;
        Vector2 offsetToUse = Vector2.zero;
        Skill hoveredSkill = null;
        Item hoveredItem = null;
        
        // Check UI elements first
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            foreach (RaycastResult result in results)
            {
                // Check if this is a skill or item UI element (only if enabled)
                Skill skill = null;
                Item item = null;
                
                if (enableSkillHovers)
                {
                    skill = GetSkillFromUIElement(result.gameObject);
                }
                
                if (enableItemHovers)
                {
                    item = GetItemFromUIElement(result.gameObject);
                }
                
                if (skill != null)
                {
                    currentlyHovering = true;
                    hoveredSkill = skill;
                    hoveredItem = null;
                    offsetToUse = new Vector2(100, -90);
                    break;
                }
                else if (item != null)
                {
                    currentlyHovering = true;
                    hoveredItem = item;
                    hoveredSkill = null;
                    offsetToUse = new Vector2(100, -90);
                    break;
                }
                
                // Check for trigger objects (legacy support)
                foreach (HoverTriggerObject triggerObj in hoverTriggerObjects)
                {
                    if (triggerObj.triggerObject != null && triggerObj.triggerObject == result.gameObject)
                    {
                        currentlyHovering = true;
                        offsetToUse = triggerObj.positionOffset;
                        break;
                    }
                }
                if (currentlyHovering) break;
            }
        }
        
        // Check 3D objects with raycasting
        if (!currentlyHovering && mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check for trigger objects
                foreach (HoverTriggerObject triggerObj in hoverTriggerObjects)
                {
                    if (triggerObj.triggerObject != null && triggerObj.triggerObject == hit.collider.gameObject)
                    {
                        currentlyHovering = true;
                        offsetToUse = triggerObj.positionOffset;
                        break;
                    }
                }
            }
        }
        
        // Update UI visibility based on hover state
        if (currentlyHovering && !isHovering)
        {
            currentOffset = offsetToUse;
            currentSkill = hoveredSkill;
            currentItem = hoveredItem;
            DisplayUI();
            isHovering = true;
        }
        else if (currentlyHovering && isHovering)
        {
            // Update offset and data if hovering over a different object
            currentOffset = offsetToUse;
            if (currentSkill != hoveredSkill || currentItem != hoveredItem)
            {
                currentSkill = hoveredSkill;
                currentItem = hoveredItem;
                UpdateHoverText();
            }
        }
        else if (!currentlyHovering && isHovering)
        {
            HideUI();
            isHovering = false;
            currentSkill = null;
            currentItem = null;
        }
    }
    
    // Get skill from UI element by checking if it matches skill buttons or icons
    Skill GetSkillFromUIElement(GameObject uiElement)
    {
        if (skillPanelManager == null || gameManager == null) return null;
        
        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null || currentUnit.Skills == null) return null;
        
        // Check if this GameObject matches any skill button or icon
        // We need to access the private arrays, so we'll use reflection or check the public fields
        // Actually, let's check the button and icon GameObjects directly
        
        // Check skill buttons
        if (skillPanelManager.skill1Button != null && (uiElement == skillPanelManager.skill1Button.gameObject || IsChildOf(uiElement, skillPanelManager.skill1Button.gameObject)))
        {
            if (currentUnit.Skills.Length > 0 && currentUnit.Skills[0] != null)
                return currentUnit.Skills[0];
        }
        if (skillPanelManager.skill2Button != null && (uiElement == skillPanelManager.skill2Button.gameObject || IsChildOf(uiElement, skillPanelManager.skill2Button.gameObject)))
        {
            if (currentUnit.Skills.Length > 1 && currentUnit.Skills[1] != null)
                return currentUnit.Skills[1];
        }
        if (skillPanelManager.skill3Button != null && (uiElement == skillPanelManager.skill3Button.gameObject || IsChildOf(uiElement, skillPanelManager.skill3Button.gameObject)))
        {
            if (currentUnit.Skills.Length > 2 && currentUnit.Skills[2] != null)
                return currentUnit.Skills[2];
        }
        if (skillPanelManager.skill4Button != null && (uiElement == skillPanelManager.skill4Button.gameObject || IsChildOf(uiElement, skillPanelManager.skill4Button.gameObject)))
        {
            if (currentUnit.Skills.Length > 3 && currentUnit.Skills[3] != null)
                return currentUnit.Skills[3];
        }
        
        // Check skill icons
        if (skillPanelManager.skill1Icon != null && (uiElement == skillPanelManager.skill1Icon.gameObject || IsChildOf(uiElement, skillPanelManager.skill1Icon.gameObject)))
        {
            if (currentUnit.Skills.Length > 0 && currentUnit.Skills[0] != null)
                return currentUnit.Skills[0];
        }
        if (skillPanelManager.skill2Icon != null && (uiElement == skillPanelManager.skill2Icon.gameObject || IsChildOf(uiElement, skillPanelManager.skill2Icon.gameObject)))
        {
            if (currentUnit.Skills.Length > 1 && currentUnit.Skills[1] != null)
                return currentUnit.Skills[1];
        }
        if (skillPanelManager.skill3Icon != null && (uiElement == skillPanelManager.skill3Icon.gameObject || IsChildOf(uiElement, skillPanelManager.skill3Icon.gameObject)))
        {
            if (currentUnit.Skills.Length > 2 && currentUnit.Skills[2] != null)
                return currentUnit.Skills[2];
        }
        if (skillPanelManager.skill4Icon != null && (uiElement == skillPanelManager.skill4Icon.gameObject || IsChildOf(uiElement, skillPanelManager.skill4Icon.gameObject)))
        {
            if (currentUnit.Skills.Length > 3 && currentUnit.Skills[3] != null)
                return currentUnit.Skills[3];
        }
        
        return null;
    }
    
    // Get item from UI element by checking if it matches item buttons or icons
    Item GetItemFromUIElement(GameObject uiElement)
    {
        if (itemPanelManager == null || inventory == null) return null;
        
        List<ItemEntry> items = inventory.Items;
        if (items == null) return null;
        
        // Check item buttons
        if (itemPanelManager.item1Button != null && (uiElement == itemPanelManager.item1Button.gameObject || IsChildOf(uiElement, itemPanelManager.item1Button.gameObject)))
        {
            if (items.Count > 0 && items[0] != null && items[0].item != null)
                return items[0].item;
        }
        if (itemPanelManager.item2Button != null && (uiElement == itemPanelManager.item2Button.gameObject || IsChildOf(uiElement, itemPanelManager.item2Button.gameObject)))
        {
            if (items.Count > 1 && items[1] != null && items[1].item != null)
                return items[1].item;
        }
        if (itemPanelManager.item3Button != null && (uiElement == itemPanelManager.item3Button.gameObject || IsChildOf(uiElement, itemPanelManager.item3Button.gameObject)))
        {
            if (items.Count > 2 && items[2] != null && items[2].item != null)
                return items[2].item;
        }
        if (itemPanelManager.item4Button != null && (uiElement == itemPanelManager.item4Button.gameObject || IsChildOf(uiElement, itemPanelManager.item4Button.gameObject)))
        {
            if (items.Count > 3 && items[3] != null && items[3].item != null)
                return items[3].item;
        }
        
        // Check item icons
        if (itemPanelManager.item1Icon != null && (uiElement == itemPanelManager.item1Icon.gameObject || IsChildOf(uiElement, itemPanelManager.item1Icon.gameObject)))
        {
            if (items.Count > 0 && items[0] != null && items[0].item != null)
                return items[0].item;
        }
        if (itemPanelManager.item2Icon != null && (uiElement == itemPanelManager.item2Icon.gameObject || IsChildOf(uiElement, itemPanelManager.item2Icon.gameObject)))
        {
            if (items.Count > 1 && items[1] != null && items[1].item != null)
                return items[1].item;
        }
        if (itemPanelManager.item3Icon != null && (uiElement == itemPanelManager.item3Icon.gameObject || IsChildOf(uiElement, itemPanelManager.item3Icon.gameObject)))
        {
            if (items.Count > 2 && items[2] != null && items[2].item != null)
                return items[2].item;
        }
        if (itemPanelManager.item4Icon != null && (uiElement == itemPanelManager.item4Icon.gameObject || IsChildOf(uiElement, itemPanelManager.item4Icon.gameObject)))
        {
            if (items.Count > 3 && items[3] != null && items[3].item != null)
                return items[3].item;
        }
        
        return null;
    }
    
    // Helper method to check if a GameObject is a child of another
    bool IsChildOf(GameObject child, GameObject parent)
    {
        if (child == null || parent == null) return false;
        Transform current = child.transform;
        while (current != null)
        {
            if (current.gameObject == parent) return true;
            current = current.parent;
        }
        return false;
    }
    
    void UpdateUIPosition()
    {
        if (hoverUIRectTransform == null || canvas == null) return;
        
        Vector2 mousePosition = Input.mousePosition;
        Vector2 localPoint;
        
        // Convert screen position to local canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );
        
        // Apply position offset for the current hovered object
        localPoint += currentOffset;
        
        // Set the position relative to the canvas
        hoverUIRectTransform.position = canvas.transform.TransformPoint(localPoint);
    }
    
    // Function to display the UI GameObject
    public void DisplayUI()
    {
        if (HoverUI != null)
        {
            HoverUI.SetActive(true);
            UpdateUIPosition();
            UpdateHoverText();
        }
    }
    
    // Function to update the hover text based on current skill or item
    void UpdateHoverText()
    {
        if (hoverText == null) return;
        
        if (currentSkill != null)
        {
            hoverText.text = FormatSkillInfo(currentSkill);
        }
        else if (currentItem != null)
        {
            hoverText.text = FormatItemInfo(currentItem);
        }
        else
        {
            hoverText.text = "";
        }
    }
    
    // Format skill information for display
    string FormatSkillInfo(Skill skill)
    {
        if (skill == null) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Target Type
        sb.AppendLine($"<b>Target:</b> {skill.targetType}");
        
        // Damage or Heal Amount
        if (skill.damage > 0)
        {
            sb.AppendLine($"<b>Damage:</b> {skill.damage}");
        }
        if (skill.healAmount > 0)
        {
            sb.AppendLine($"<b>Heal:</b> {skill.healAmount}");
        }
        
        // Description
        if (!string.IsNullOrEmpty(skill.description))
        {
            sb.AppendLine();
            sb.AppendLine(skill.description);
        }
        
        return sb.ToString();
    }
    
    // Format item information for display
    string FormatItemInfo(Item item)
    {
        if (item == null) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Target Type
        sb.AppendLine($"<b>Target:</b> {item.targetType}");
        
        // Consumable Subtype and Amount
        if (item.itemType == ItemType.Consumable)
        {
            sb.AppendLine($"<b>Type:</b> {item.consumableSubtype}");
            
            // Show amount based on subtype
            if (item.consumableSubtype == ConsumableSubtype.Heal && item.healAmount > 0)
            {
                sb.AppendLine($"<b>Amount:</b> {item.healAmount}");
            }
            // Note: Currently items only have healAmount field. If damage/status amounts are added later, extend this.
        }
        
        // Description
        if (!string.IsNullOrEmpty(item.itemDescription))
        {
            sb.AppendLine();
            sb.AppendLine(item.itemDescription);
        }
        
        return sb.ToString();
    }
    
    // Function to hide the UI GameObject
    public void HideUI()
    {
        if (HoverUI != null)
        {
            HoverUI.SetActive(false);
        }
    }
}
