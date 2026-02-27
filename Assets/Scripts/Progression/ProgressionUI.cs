using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressionUI : MonoBehaviour
{
    public static ProgressionUI Instance;

    [System.Serializable]
    public class TrackUIElements
    {
        [Header("Track Assignment")]
        public string trackId;

        [Header("Current Goal Display")]
        public GameObject goalPanel;
        public TMP_Text goalText;
        public Image trackColorIndicator;

        [Header("Progress Display (used for goal progress OR unlock progress)")]
        public GameObject progressPanel;
        public TMP_Text progressText;
        public Slider progressSlider;
        public Image progressIcon;  // Shows goal icon or unlock silhouette

        public GameObject unlockNotificationPanel;
        public TMP_Text unlockNotificationText;
        public Image unlockNotificationIcon;
        public float notificationDuration = 3f;

        [Header("Manual Track / Puzzle Button")]
        public Button puzzleRequestButton;      // "Give me a Puzzle" button
        public TMP_Text puzzleButtonText;
        public GameObject gatingInfoPanel;       // shows gating message when next puzzle is locked
        public TMP_Text gatingInfoText;
    }

    [Header("Track UI Elements")]
    [SerializeField] private List<TrackUIElements> trackUIs = new List<TrackUIElements>();

    private Dictionary<string, TrackUIElements> trackUIMap = new Dictionary<string, TrackUIElements>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var ui in trackUIs)
            if (!string.IsNullOrEmpty(ui.trackId))
                trackUIMap[ui.trackId] = ui;
    }

    void Start()
    {
        ProgressHandler.OnNewGoalStarted += OnNewGoalStarted;
        ProgressHandler.OnNextUnlockProgressChanged += OnNextUnlockProgressChanged;
        ProgressHandler.OnRewardsGranted += OnRewardsGranted;
        ProgressHandler.OnTrackProgressChanged += OnTrackProgressChanged;
        ProgressHandler.OnTrackCompleted += OnTrackCompleted;
        ProgressHandler.OnAnyProgressChanged += RefreshAllUI;
        ProgressHandler.OnPuzzleCompleted += OnPuzzleCompleted;

        // Wire up puzzle request buttons
        foreach (var ui in trackUIs)
        {
            if (ui.puzzleRequestButton != null)
            {
                string capturedId = ui.trackId;
                ui.puzzleRequestButton.onClick.AddListener(() => OnPuzzleRequestClicked(capturedId));
            }
        }

        RefreshAllUI();
    }

    void OnDestroy()
    {
        ProgressHandler.OnNewGoalStarted -= OnNewGoalStarted;
        ProgressHandler.OnNextUnlockProgressChanged -= OnNextUnlockProgressChanged;
        ProgressHandler.OnRewardsGranted -= OnRewardsGranted;
        ProgressHandler.OnTrackProgressChanged -= OnTrackProgressChanged;
        ProgressHandler.OnTrackCompleted -= OnTrackCompleted;
        ProgressHandler.OnAnyProgressChanged -= RefreshAllUI;
        ProgressHandler.OnPuzzleCompleted -= OnPuzzleCompleted;
}

    void Update()
    {
        // Continuously update goal progress for tracks that show it
        if (ProgressHandler.Instance == null) return;

        foreach (var kvp in trackUIMap)
        {
            var state = ProgressHandler.Instance.GetTrackState(kvp.Key);
            if (state != null && state.track.showGoalProgress && state.currentGoal != null)
            {
                UpdateGoalProgressDisplay(kvp.Value, state.currentGoal);
            }
        }
    }

    void OnNewGoalStarted(string trackId, Goal goal)
    {
        if (trackUIMap.TryGetValue(trackId, out var ui))
            UpdateGoalDisplay(ui, goal);
    }

    void OnNextUnlockProgressChanged(string trackId, NextUnlockInfo info)
    {
        if (trackUIMap.TryGetValue(trackId, out var ui))
            RefreshTrackUI(trackId, ui);
    }

    void OnRewardsGranted(string trackId, List<Unlockable> rewards)
    {
        if (rewards.Count > 0)
            ShowUnlockNotification(rewards[0], trackId);
    }

    void OnTrackProgressChanged(string trackId)
    {
        if (trackUIMap.TryGetValue(trackId, out var ui))
            RefreshTrackUI(trackId, ui);
    }

    void OnTrackCompleted(string trackId)
    {
        if (trackUIMap.TryGetValue(trackId, out var ui))
        {
            if (ui.progressPanel != null) ui.progressPanel.SetActive(false);
        }
    }

    void OnPuzzleRequestClicked(string trackId)
{
    if (ProgressHandler.Instance == null) return;

    bool started = ProgressHandler.Instance.RequestNextGoal(trackId);
    if (started)
    {
        RefreshManualTrackUI(trackId);
    }
}

void OnPuzzleCompleted(string trackId)
{
    RefreshManualTrackUI(trackId);
}

void RefreshManualTrackUI(string trackId)
{
    if (!trackUIMap.TryGetValue(trackId, out var ui)) return;

    var state = ProgressHandler.Instance?.GetTrackState(trackId);
    if (state == null) return;

    bool isManual = state.track.requiresManualStart;
    bool hasActiveGoal = state.currentGoal != null;
    bool isComplete = state.IsComplete;

    if (!isManual)
    {
        // Non-manual tracks: hide puzzle button
        if (ui.puzzleRequestButton != null)
            ui.puzzleRequestButton.gameObject.SetActive(false);
        if (ui.gatingInfoPanel != null)
            ui.gatingInfoPanel.SetActive(false);
        return;
    }

    if (isComplete)
    {
        // Track complete
        if (ui.puzzleRequestButton != null)
            ui.puzzleRequestButton.gameObject.SetActive(false);
        if (ui.goalPanel != null)
            ui.goalPanel.SetActive(false);
        if (ui.progressPanel != null)
            ui.progressPanel.SetActive(false);
        if (ui.gatingInfoPanel != null)
            ui.gatingInfoPanel.SetActive(false);
        return;
    }

    if (hasActiveGoal)
    {
        // Puzzle is active: hide button, show goal + progress
        if (ui.puzzleRequestButton != null)
            ui.puzzleRequestButton.gameObject.SetActive(false);
        if (ui.gatingInfoPanel != null)
            ui.gatingInfoPanel.SetActive(false);
        UpdateGoalDisplay(ui, state.currentGoal);
        if (state.track.showGoalProgress)
            UpdateGoalProgressDisplay(ui, state.currentGoal);
    }
    else
    {
        // No active puzzle: show button (or gating info), hide goal/progress
        if (ui.goalPanel != null)
            ui.goalPanel.SetActive(false);
        if (ui.progressPanel != null)
            ui.progressPanel.SetActive(false);

        string gatingInfo = ProgressHandler.Instance.GetGatingInfoForNextGoal(trackId);
        if (gatingInfo != null)
        {
            // Next puzzle is gated — show gating info, hide button
            if (ui.puzzleRequestButton != null)
                ui.puzzleRequestButton.gameObject.SetActive(false);
            if (ui.gatingInfoPanel != null)
            {
                ui.gatingInfoPanel.SetActive(true);
                if (ui.gatingInfoText != null)
                    ui.gatingInfoText.text = gatingInfo;
            }
        }
        else
        {
            // Next puzzle is available — show button
            if (ui.gatingInfoPanel != null)
                ui.gatingInfoPanel.SetActive(false);
            if (ui.puzzleRequestButton != null)
                ui.puzzleRequestButton.gameObject.SetActive(true);
        }
    }
}

    void RefreshAllUI()
    {
        if (ProgressHandler.Instance == null) return;

        foreach (var kvp in trackUIMap)
            RefreshTrackUI(kvp.Key, kvp.Value);

    }

    void RefreshTrackUI(string trackId, TrackUIElements ui)
    {
        var state = ProgressHandler.Instance.GetTrackState(trackId);
        if (state == null) return;

        UpdateGoalDisplay(ui, state.currentGoal);

        if (ui.trackColorIndicator != null)
            ui.trackColorIndicator.color = state.track.trackColor;

        if (state.IsComplete)
        {
            if (ui.progressPanel != null) ui.progressPanel.SetActive(false);
        }
        else if (state.track.showGoalProgress)
        {
            UpdateGoalProgressDisplay(ui, state.currentGoal);
        }
        else
        {
            UpdateUnlockProgressDisplay(ui, state.cachedNextUnlock);
        }
    }

    void UpdateGoalDisplay(TrackUIElements ui, Goal goal)
    {
        if (goal != null)
        {
            if (ui.goalPanel != null) ui.goalPanel.SetActive(true);
            if (ui.goalText != null) ui.goalText.text = goal.displayText;
        }
        else if (ui.goalPanel != null) ui.goalPanel.SetActive(false);
    }

    void UpdateGoalProgressDisplay(TrackUIElements ui, Goal goal)
    {
        if (ui.progressPanel != null) ui.progressPanel.SetActive(true);

        if (goal == null || !goal.showProgressUI)
        {
            if (ui.progressPanel != null) ui.progressPanel.SetActive(false);
            return;
        }
        
        string progressText = goal.GetProgressText();
        if (ui.progressText != null)
            ui.progressText.text = string.IsNullOrEmpty(progressText) ? "" : progressText;

        if (ui.progressSlider != null)
            ui.progressSlider.value = goal.GetProgressNormalized();

        if (ui.progressIcon != null && goal.goalIcon != null)
            ui.progressIcon.sprite = goal.goalIcon;
    }

    void UpdateUnlockProgressDisplay(TrackUIElements ui, NextUnlockInfo info)
    {
        if (info == null || info.unlockable == null)
        {
            if (ui.progressPanel != null) ui.progressPanel.SetActive(false);
            return;
        }

        if (ui.progressPanel != null) ui.progressPanel.SetActive(true);
        if (ui.progressText != null) ui.progressText.text = info.GetProgressText();

        if (ui.progressSlider != null)
            ui.progressSlider.value = info.GetProgressNormalized();

        if (ui.progressIcon != null && info.unlockable.icon != null)
            ui.progressIcon.sprite = info.unlockable.icon;
    }

    void ShowUnlockNotification(Unlockable unlockable, string trackId)
    {
        if (trackUIMap[trackId].unlockNotificationPanel == null) return;

        var state = ProgressHandler.Instance?.GetTrackState(trackId);
        string trackName = state?.track.displayName ?? "";

        if (trackUIMap[trackId].unlockNotificationText != null)
            trackUIMap[trackId].unlockNotificationText.text = $"{unlockable.displayName} Unlocked!";

        if (trackUIMap[trackId].unlockNotificationIcon != null && unlockable.icon != null)
            trackUIMap[trackId].unlockNotificationIcon.sprite = unlockable.icon;

        trackUIMap[trackId].unlockNotificationPanel.SetActive(true);
    }

    public void HideNotification(string trackId)
    {
        if (trackUIMap[trackId].unlockNotificationPanel != null)
            trackUIMap[trackId].unlockNotificationPanel.SetActive(false);
    }
    IEnumerator HideNotificationAfterDelay(string trackId)
    {
        yield return new WaitForSeconds(trackUIMap[trackId].notificationDuration);
        if (trackUIMap[trackId].unlockNotificationPanel != null) trackUIMap[trackId].unlockNotificationPanel.SetActive(false);
    }
}