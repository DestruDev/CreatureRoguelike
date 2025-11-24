using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject infoPanelUI;
    [SerializeField] private TextMeshProUGUI infoText;
    
    [Header("Settings")]
    [SerializeField] private bool enableSkillInfo = true;
    [SerializeField] private bool enableItemInfo = true;
    
    // References to managers
    private ActionPanelManager actionPanelManager;
    private SkillPanelManager skillPanelManager;
    private ItemPanelManager itemPanelManager;
    private GameManager gameManager;
    private Inventory inventory;
    private Selection selection;
    
    private void Start()
    {
        // Find all necessary managers
        actionPanelManager = FindFirstObjectByType<ActionPanelManager>();
        skillPanelManager = FindFirstObjectByType<SkillPanelManager>();
        itemPanelManager = FindFirstObjectByType<ItemPanelManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        inventory = FindFirstObjectByType<Inventory>();
        selection = FindFirstObjectByType<Selection>();
        
        // Hide info panel by default
        if (infoPanelUI != null)
        {
            infoPanelUI.SetActive(false);
        }
    }
    
    private void Update()
    {
        UpdateInfoPanel();
    }
    
    void UpdateInfoPanel()
    {
        bool shouldShow = false;
        Skill currentSkill = null;
        Item currentItem = null;
        int currentSkillIndex = -1;
        
        // Check if we're in selection mode (after selecting a skill, before selecting target)
        if (enableSkillInfo && skillPanelManager != null && skillPanelManager.IsInSelectionMode())
        {
            // Use the skill stored in SkillPanelManager during selection mode
            currentSkill = skillPanelManager.GetCurrentSkill();
            currentSkillIndex = skillPanelManager.GetSelectedSkillIndex();
            if (currentSkill != null)
            {
                shouldShow = true;
            }
        }
        // Check if skills panel is active
        else if (enableSkillInfo && actionPanelManager != null && actionPanelManager.SkillsPanel != null && actionPanelManager.SkillsPanel.activeSelf)
        {
            var skillData = GetSelectedSkill();
            currentSkill = skillData.skill;
            currentSkillIndex = skillData.index;
            if (currentSkill != null)
            {
                shouldShow = true;
            }
        }
        // Check if items panel is active
        else if (enableItemInfo && actionPanelManager != null && actionPanelManager.ItemsPanel != null && actionPanelManager.ItemsPanel.activeSelf)
        {
            currentItem = GetSelectedItem();
            if (currentItem != null)
            {
                shouldShow = true;
            }
        }
        
        // Update UI visibility and content
        if (shouldShow)
        {
            if (infoPanelUI != null && !infoPanelUI.activeSelf)
            {
                infoPanelUI.SetActive(true);
            }
            
            if (infoText != null)
            {
                if (currentSkill != null)
                {
                    infoText.text = FormatSkillInfo(currentSkill, currentSkillIndex);
                }
                else if (currentItem != null)
                {
                    infoText.text = FormatItemInfo(currentItem);
                }
                else
                {
                    infoText.text = "";
                }
            }
        }
        else
        {
            if (infoPanelUI != null && infoPanelUI.activeSelf)
            {
                infoPanelUI.SetActive(false);
            }
        }
    }
    
    // Get the currently selected skill from the selection system (returns skill and index)
    (Skill skill, int index) GetSelectedSkill()
    {
        if (skillPanelManager == null || gameManager == null || selection == null) return (null, -1);
        
        Unit currentUnit = gameManager.GetCurrentUnit();
        if (currentUnit == null || currentUnit.Skills == null) return (null, -1);
        
        // Check if there's a selected button
        if (selection.IsValidSelection())
        {
            object selectedItem = selection.CurrentSelection;
            if (selectedItem is UnityEngine.UI.Button button)
            {
                // Find which skill button was selected
                if (button == skillPanelManager.skill1Button && currentUnit.Skills.Length > 0 && currentUnit.Skills[0] != null)
                    return (currentUnit.Skills[0], 0);
                if (button == skillPanelManager.skill2Button && currentUnit.Skills.Length > 1 && currentUnit.Skills[1] != null)
                    return (currentUnit.Skills[1], 1);
                if (button == skillPanelManager.skill3Button && currentUnit.Skills.Length > 2 && currentUnit.Skills[2] != null)
                    return (currentUnit.Skills[2], 2);
                if (button == skillPanelManager.skill4Button && currentUnit.Skills.Length > 3 && currentUnit.Skills[3] != null)
                    return (currentUnit.Skills[3], 3);
            }
        }
        
        // If no selection, return first available skill
        for (int i = 0; i < currentUnit.Skills.Length && i < 4; i++)
        {
            if (currentUnit.Skills[i] != null)
            {
                return (currentUnit.Skills[i], i);
            }
        }
        
        return (null, -1);
    }
    
    // Get the currently selected item from the selection system
    Item GetSelectedItem()
    {
        if (itemPanelManager == null || inventory == null || selection == null) return null;
        
        List<ItemEntry> items = inventory.Items;
        if (items == null || items.Count == 0) return null;
        
        // Check if there's a selected button
        if (selection.IsValidSelection())
        {
            object selectedItem = selection.CurrentSelection;
            if (selectedItem is UnityEngine.UI.Button button)
            {
                // Find which item button was selected
                if (button == itemPanelManager.item1Button && items.Count > 0 && items[0] != null && items[0].item != null)
                    return items[0].item;
                if (button == itemPanelManager.item2Button && items.Count > 1 && items[1] != null && items[1].item != null)
                    return items[1].item;
                if (button == itemPanelManager.item3Button && items.Count > 2 && items[2] != null && items[2].item != null)
                    return items[2].item;
                if (button == itemPanelManager.item4Button && items.Count > 3 && items[3] != null && items[3].item != null)
                    return items[3].item;
            }
        }
        
        // If no selection, return first available item
        if (items.Count > 0 && items[0] != null && items[0].item != null)
        {
            return items[0].item;
        }
        
        return null;
    }
    
    // Format skill information for display (same as Hover.cs, but with unusability info)
    string FormatSkillInfo(Skill skill, int skillIndex)
    {
        if (skill == null) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Check if skill is unusable and get reason
        string unusableReason = "";
        if (gameManager != null && skillIndex >= 0)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            if (currentUnit != null && !currentUnit.CanUseSkill(skillIndex))
            {
                unusableReason = currentUnit.GetSkillUnusableReason(skillIndex);
            }
        }
        
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
        
        // Show unusability reason if skill cannot be used
        if (!string.IsNullOrEmpty(unusableReason))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=red><b>Cannot use:</b> {unusableReason}</color>");
        }
        
        // Description
        if (!string.IsNullOrEmpty(skill.description))
        {
            sb.AppendLine();
            sb.AppendLine(skill.description);
        }
        
        return sb.ToString();
    }
    
    // Format item information for display (same as Hover.cs, but with unusability info)
    string FormatItemInfo(Item item)
    {
        if (item == null) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Check if item is unusable and get reason
        string unusableReason = "";
        if (itemPanelManager != null && gameManager != null)
        {
            Unit currentUnit = gameManager.GetCurrentUnit();
            if (currentUnit != null)
            {
                unusableReason = itemPanelManager.GetItemUnusableReason(item, currentUnit);
            }
        }
        
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
        
        // Show unusability reason if item cannot be used
        if (!string.IsNullOrEmpty(unusableReason))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=red><b>Cannot use:</b> {unusableReason}</color>");
        }
        
        // Description
        if (!string.IsNullOrEmpty(item.itemDescription))
        {
            sb.AppendLine();
            sb.AppendLine(item.itemDescription);
        }
        
        return sb.ToString();
    }
}
