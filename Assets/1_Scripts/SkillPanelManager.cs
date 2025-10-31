using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillPanelManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;

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

    private TextMeshProUGUI[] skillNames;
    private Image[] skillIcons;

    private void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Initialize arrays for easier iteration
        skillNames = new TextMeshProUGUI[] { skill1Name, skill2Name, skill3Name, skill4Name };
        skillIcons = new Image[] { skill1Icon, skill2Icon, skill3Icon, skill4Icon };

        // Update skills on start
        UpdateSkills();
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
}
