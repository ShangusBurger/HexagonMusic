using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small toggle-able stats window. A button in a corner opens/closes a panel
/// showing lifetime tower interactions and sound changes.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("Toggle Button")]
    [SerializeField] private Button statsToggleButton;

    [Header("Stats Panel")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text towerInteractionsText;
    [SerializeField] private TMP_Text soundChangesText;

    void Start()
    {
        if (statsToggleButton != null)
            statsToggleButton.onClick.AddListener(TogglePanel);

        if (statsPanel != null)
            statsPanel.SetActive(false);

        PlayerStats.OnStatsChanged += RefreshDisplay;
    }

    void OnDestroy()
    {
        PlayerStats.OnStatsChanged -= RefreshDisplay;
    }

    void TogglePanel()
    {
        if (statsPanel == null) return;

        bool show = !statsPanel.activeSelf;
        statsPanel.SetActive(show);

        if (show)
            RefreshDisplay();
    }

    void RefreshDisplay()
    {
        if (PlayerStats.Instance == null) return;

        if (towerInteractionsText != null)
            towerInteractionsText.text = 
                $"Towers Placed / Deleted: {PlayerStats.Instance.TotalTowerInteractions}";

        if (soundChangesText != null)
            soundChangesText.text = 
                $"Sound Changes Made: {PlayerStats.Instance.TotalSoundChanges}";
    }
}