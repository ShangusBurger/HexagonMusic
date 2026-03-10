using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the save/load system.
/// Wire up buttons in your pause menu and main menu to this component.
/// </summary>
public class SaveLoadUI : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;       // The pause menu root panel (menuContents)
    [SerializeField] private MenuUI menuUI;                   // Reference to MenuUI for full menu dismiss
    [SerializeField] private Button saveProgressButton;
    [SerializeField] private Button saveMapButton;
    [SerializeField] private Button loadMapButton;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Map Save Panel")]
    [SerializeField] private GameObject mapSavePanel;
    [SerializeField] private TMP_InputField mapNameInput;
    [SerializeField] private Button confirmSaveMapButton;     // Saves locally
    [SerializeField] private Button copyToClipboardButton;    // Copies to clipboard
    [SerializeField] private Button cancelSaveMapButton;

    [Header("Map Load Panel")]
    [SerializeField] private GameObject mapLoadPanel;
    [SerializeField] private Transform savedMapListContent;   // ScrollView > Viewport > Content
    [SerializeField] private GameObject savedMapEntryPrefab;  // Prefab: Button + TMP_Text + Delete Button
    [SerializeField] private TMP_InputField pasteMapInput;
    [SerializeField] private Button importPasteButton;        // "Import" — adds to list, does NOT load
    [SerializeField] private Button clearAllMapsButton;
    [SerializeField] private Button cancelLoadButton;

    [Header("Confirmation Popup")]
    [SerializeField] private GameObject confirmPopupPanel;
    [SerializeField] private TMP_Text confirmPopupText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackPanel;        // Background panel behind the text
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float feedbackDuration = 2f;

    // ── Confirmation callback ─────────────────────────────────────────
    private System.Action pendingConfirmAction;

    void Start()
    {
        // ── Progress ──
        if (saveProgressButton != null)
            saveProgressButton.onClick.AddListener(OnSaveProgressClicked);

        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(OnResetProgressClicked);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        // ── Map Save ──
        if (saveMapButton != null)
            saveMapButton.onClick.AddListener(OnSaveMapClicked);
        if (confirmSaveMapButton != null)
            confirmSaveMapButton.onClick.AddListener(OnConfirmSaveMap);
        if (copyToClipboardButton != null)
            copyToClipboardButton.onClick.AddListener(OnCopyToClipboard);
        if (cancelSaveMapButton != null)
            cancelSaveMapButton.onClick.AddListener(() => CloseSubPanel(mapSavePanel));

        // ── Map Load ──
        if (loadMapButton != null)
            loadMapButton.onClick.AddListener(OnLoadMapClicked);
        if (importPasteButton != null)
            importPasteButton.onClick.AddListener(OnImportPasteClicked);
        if (clearAllMapsButton != null)
            clearAllMapsButton.onClick.AddListener(OnClearAllMapsClicked);
        if (cancelLoadButton != null)
            cancelLoadButton.onClick.AddListener(() => CloseSubPanel(mapLoadPanel));

        // ── Confirmation popup ──
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        // ── Hide panels initially ──
        if (mapSavePanel != null) mapSavePanel.SetActive(false);
        if (mapLoadPanel != null) mapLoadPanel.SetActive(false);
        if (confirmPopupPanel != null) confirmPopupPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════
    //  PROGRESS
    // ══════════════════════════════════════════════════════════════════

    void OnSaveProgressClicked()
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.SaveProgress();
        ShowFeedback("Progress saved!");
    }

    void OnResetProgressClicked()
    {
        ShowConfirmation(
            "Are you sure you want to reset ALL progress?\nThis cannot be undone!",
            () =>
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.ResetAllProgress();
                ShowFeedback("All progress has been reset.");
            }
        );
    }

    void OnFullscreenToggled(bool isFullscreen)
    {
        if (isFullscreen)
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        else
            Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    // ══════════════════════════════════════════════════════════════════
    //  MAP SAVE
    // ══════════════════════════════════════════════════════════════════

    void OnSaveMapClicked()
    {
        if (mapSavePanel != null)
        {
            OpenSubPanel(mapSavePanel);
            if (mapNameInput != null) mapNameInput.text = "";
        }
    }

    void OnConfirmSaveMap()
    {
        if (SaveManager.Instance == null) return;

        string mapName = mapNameInput != null ? mapNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(mapName))
        {
            ShowFeedback("Enter a name for your map!");
            return;
        }

        SaveManager.Instance.SaveMapToLocal(mapName);
        ShowFeedback($"'{mapName}' saved!");
        CloseSubPanel(mapSavePanel);
    }

    void OnCopyToClipboard()
    {
        if (SaveManager.Instance == null) return;

        string mapName = mapNameInput != null ? mapNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(mapName))
            mapName = "Untitled";

        string encoded = SaveManager.Instance.CopyMapToClipboard(mapName);
        ShowFeedback($"Copied to clipboard! ({encoded.Length} chars)");
    }

    // ══════════════════════════════════════════════════════════════════
    //  MAP LOAD
    // ══════════════════════════════════════════════════════════════════

    void OnLoadMapClicked()
    {
        if (mapLoadPanel != null)
        {
            OpenSubPanel(mapLoadPanel);
            if (pasteMapInput != null) pasteMapInput.text = "";
            RefreshSavedMapList();
        }
    }

    /// <summary>
    /// Imports a pasted map code into the local saved maps folder
    /// and refreshes the scroll list. Does NOT load into the game.
    /// </summary>
    void OnImportPasteClicked()
    {
        if (SaveManager.Instance == null || pasteMapInput == null) return;

        string pasted = pasteMapInput.text.Trim();
        if (string.IsNullOrEmpty(pasted))
        {
            ShowFeedback("Paste a map code first!");
            return;
        }

        string importedName = SaveManager.Instance.ImportMap(pasted);
        if (importedName == null)
        {
            ShowFeedback("Invalid map code.");
            return;
        }

        ShowFeedback($"Imported '{importedName}'!");
        pasteMapInput.text = "";
        RefreshSavedMapList();
    }

    void OnClearAllMapsClicked()
    {
        ShowConfirmation(
            "Delete ALL saved maps?\nThis cannot be undone!",
            () =>
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.DeleteAllMaps();
                RefreshSavedMapList();
                ShowFeedback("All saved maps deleted.");
            }
        );
    }

    void RefreshSavedMapList()
    {
        if (savedMapListContent == null || savedMapEntryPrefab == null)
        {
            Debug.LogWarning("[SaveLoadUI] Missing savedMapListContent or savedMapEntryPrefab reference.");
            return;
        }

        // Destroy old entries immediately so VerticalLayoutGroup doesn't see ghosts
        for (int i = savedMapListContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(savedMapListContent.GetChild(i).gameObject);

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveLoadUI] SaveManager.Instance is null.");
            return;
        }

        List<string> mapNames = SaveManager.Instance.GetSavedMapNames();
        Debug.Log($"[SaveLoadUI] Found {mapNames.Count} saved maps.");

        foreach (string name in mapNames)
        {
            GameObject entry = Instantiate(savedMapEntryPrefab, savedMapListContent);
            entry.SetActive(true);

            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = name;

            Button[] buttons = entry.GetComponentsInChildren<Button>(true);
            string capturedName = name;

            if (buttons.Length >= 2)
            {
                // First button = Load, Second button = Delete
                buttons[0].onClick.AddListener(() => OnLoadSavedMap(capturedName));
                buttons[1].onClick.AddListener(() => OnDeleteSavedMap(capturedName));
            }
            else if (buttons.Length == 1)
            {
                buttons[0].onClick.AddListener(() => OnLoadSavedMap(capturedName));
            }
            else
            {
                Debug.LogWarning("[SaveLoadUI] Entry prefab has no Button components.");
            }
        }

        // Force layout rebuild so the ScrollView recalculates content size
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
            savedMapListContent.GetComponent<RectTransform>());
    }

    void OnLoadSavedMap(string mapName)
    {
        if (SaveManager.Instance == null) return;

        string encoded = SaveManager.Instance.ReadMapFileRaw(mapName);
        if (string.IsNullOrEmpty(encoded))
        {
            ShowFeedback($"Failed to read '{mapName}'.");
            return;
        }

        // Check for locked content
        string warning = SaveManager.Instance.CheckMapForLockedContent(encoded);
        if (warning != null)
        {
            ShowConfirmation(warning, () => ExecuteLoadMap(mapName, encoded));
        }
        else
        {
            ExecuteLoadMap(mapName, encoded);
        }
    }

    void ExecuteLoadMap(string mapName, string encoded)
    {
        bool success = SaveManager.Instance.LoadMap(encoded);
        ShowFeedback(success ? $"Loaded '{mapName}'!" : $"Failed to load '{mapName}'.");

        // Dismiss everything — ResumeGame closes sub-panels + hides the menu
        if (menuUI != null)
            menuUI.ResumeGame();
        else
            CloseSubPanel(mapLoadPanel);
    }

    void OnDeleteSavedMap(string mapName)
    {
        ShowConfirmation(
            $"Delete map '{mapName}'?",
            () =>
            {
                if (SaveManager.Instance != null)
                    SaveManager.Instance.DeleteMapFile(mapName);
                RefreshSavedMapList();
                ShowFeedback($"Deleted '{mapName}'.");
            }
        );
    }

    // ══════════════════════════════════════════════════════════════════
    //  CONFIRMATION POPUP
    // ══════════════════════════════════════════════════════════════════

    void ShowConfirmation(string message, System.Action onConfirm)
    {
        pendingConfirmAction = onConfirm;

        if (confirmPopupText != null)
            confirmPopupText.text = message;

        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(true);
    }

    void OnConfirmYes()
    {
        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(false);

        pendingConfirmAction?.Invoke();
        pendingConfirmAction = null;
    }

    void OnConfirmNo()
    {
        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(false);

        pendingConfirmAction = null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  SUB-PANEL MANAGEMENT
    //  Hides the pause menu content while a sub-panel is open,
    //  and restores it when the sub-panel closes.
    // ══════════════════════════════════════════════════════════════════

    private GameObject activeSubPanel = null;

    void OpenSubPanel(GameObject panel)
    {
        if (panel == null) return;

        // Hide the pause menu buttons (but keep the MenuUI canvas alive)
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        activeSubPanel = panel;
        panel.SetActive(true);
    }

    void CloseSubPanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);

        // Also close any open confirmation popup
        if (confirmPopupPanel != null)
            confirmPopupPanel.SetActive(false);
        pendingConfirmAction = null;

        activeSubPanel = null;

        // Restore the pause menu buttons
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Called by MenuUI when Escape is pressed while the menu is open.
    /// Returns true if a sub-panel was closed (so MenuUI should NOT
    /// also close the whole menu).
    /// </summary>
    public bool TryCloseActiveSubPanel()
    {
        // Close confirmation popup first if it's open
        if (confirmPopupPanel != null && confirmPopupPanel.activeSelf)
        {
            OnConfirmNo();
            return true;
        }

        if (activeSubPanel != null && activeSubPanel.activeSelf)
        {
            CloseSubPanel(activeSubPanel);
            return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ══════════════════════════════════════════════════════════════════

    void ShowFeedback(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
        else if (statusText != null)
            statusText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideFeedback));
        Invoke(nameof(HideFeedback), feedbackDuration);
    }

    void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
        else if (statusText != null)
            statusText.gameObject.SetActive(false);
    }
}