using UnityEngine;
using UnityEngine.UI;

public class ActionPanelManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject ActionPanel;
    public GameObject SkillsPanel;
    public GameObject ItemsPanel;

    [Header("Buttons")]
    public Button SkillsButton;
    public Button ItemsButton;

    private void Start()
    {
        // Set initial state - only ActionPanel visible
        ShowActionPanel();

        // Subscribe to button clicks
        if (SkillsButton != null)
        {
            SkillsButton.onClick.AddListener(ShowSkillsPanel);
        }

        if (ItemsButton != null)
        {
            ItemsButton.onClick.AddListener(ShowItemsPanel);
        }
    }

    private void Update()
    {
        // Check for ESC key press to return to ActionPanel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If SkillsPanel or ItemsPanel is active, return to ActionPanel
            if (SkillsPanel != null && SkillsPanel.activeSelf)
            {
                ShowActionPanel();
            }
            else if (ItemsPanel != null && ItemsPanel.activeSelf)
            {
                ShowActionPanel();
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from button clicks to prevent memory leaks
        if (SkillsButton != null)
        {
            SkillsButton.onClick.RemoveListener(ShowSkillsPanel);
        }

        if (ItemsButton != null)
        {
            ItemsButton.onClick.RemoveListener(ShowItemsPanel);
        }
    }

    public void ShowActionPanel()
    {
        // Hide all panels first
        HideAllPanels();

        // Show ActionPanel
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(true);
        }
    }

    public void ShowSkillsPanel()
    {
        // Hide all panels first
        HideAllPanels();

        // Show SkillsPanel
        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(true);
        }
    }

    public void ShowItemsPanel()
    {
        // Hide all panels first
        HideAllPanels();

        // Show ItemsPanel
        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(true);
        }
    }

    private void HideAllPanels()
    {
        // Hide all panels to ensure only one is visible at a time
        if (ActionPanel != null)
        {
            ActionPanel.SetActive(false);
        }

        if (SkillsPanel != null)
        {
            SkillsPanel.SetActive(false);
        }

        if (ItemsPanel != null)
        {
            ItemsPanel.SetActive(false);
        }
    }
}
