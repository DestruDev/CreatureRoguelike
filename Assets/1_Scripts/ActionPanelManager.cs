using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionPanelManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject ActionPanel;
    public GameObject SkillsPanel;
    public GameObject ItemsPanel;

    [Header("Buttons")]
    public Button SkillsButton;
    public Button ItemsButton;
    public Button EndTurnButton;

    [Header("References")]
    private GameManager gameManager;
    private TurnOrder turnOrder;

    private void Start()
    {
        // Find GameManager
        gameManager = FindFirstObjectByType<GameManager>();

        // Find TurnOrder
        turnOrder = FindFirstObjectByType<TurnOrder>();

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

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.AddListener(EndTurn);
        }
    }

    private void Update()
    {
        // Check for ESC key press or right-click to return to ActionPanel
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
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

        // Hide ActionPanel during enemy turns
        UpdatePanelVisibility();
    }

    /// <summary>
    /// Updates panel visibility based on whose turn it is
    /// </summary>
    private void UpdatePanelVisibility()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
        }

        Unit currentUnit = gameManager.GetCurrentUnit();

        // If it's an enemy's turn, hide all panels
        if (currentUnit != null && currentUnit.IsEnemy)
        {
            HideAllPanels();
        }
        // If it's a creature's turn and no panels are visible, show ActionPanel
        else if (currentUnit != null && currentUnit.IsCreature)
        {
            // Only show ActionPanel if no other panel is visible
            if (ActionPanel != null && !ActionPanel.activeSelf && 
                (SkillsPanel == null || !SkillsPanel.activeSelf) && 
                (ItemsPanel == null || !ItemsPanel.activeSelf))
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

        if (EndTurnButton != null)
        {
            EndTurnButton.onClick.RemoveListener(EndTurn);
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

    /// <summary>
    /// Ends the current turn and advances to the next unit's turn
    /// </summary>
    public void EndTurn()
    {
        // Start the current unit's turn (reduce cooldowns) if it's a creature's turn
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        Unit currentUnit = gameManager != null ? gameManager.GetCurrentUnit() : null;
        if (currentUnit != null && currentUnit.IsCreature)
        {
            currentUnit.StartTurn();
        }

        // Advance to next turn
        if (turnOrder == null)
        {
            turnOrder = FindFirstObjectByType<TurnOrder>();
        }

        if (turnOrder != null)
        {
            turnOrder.AdvanceToNextTurn();
        }
        else
        {
            Debug.LogWarning("ActionPanelManager: Cannot end turn - TurnOrder not found!");
        }
    }
}
