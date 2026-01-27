using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProgressionUI : MonoBehaviour
{
    public static ProgressionUI Instance;

    [Header("Current Goal Display")]
    [SerializeField] private GameObject goalPanel;
    [SerializeField] private TMP_Text goalText;

    [Header("Next Unlock Display")]
    [SerializeField] private GameObject nextUnlockPanel;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image unlockIconSilhouette;

    [Header("Unlock Notification")]
    [SerializeField] private GameObject unlockNotificationPanel;
    [SerializeField] private TMP_Text unlockNotificationText;
    [SerializeField] private Image unlockNotificationIcon;
    [SerializeField] private float notificationDuration = 3f;

    [Header("All Complete Display")]
    [SerializeField] private GameObject allCompletePanel;

    [Header("Progress Bar Colors")]
    [SerializeField] private Image progressFillImage;
    [SerializeField] private Color normalColor = Color.yellow;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ProgressHandler.OnNewGoalStarted += OnNewGoalStarted;
        ProgressHandler.OnNextUnlockProgressChanged += OnNextUnlockProgressChanged;
        ProgressHandler.OnRewardsGranted += OnRewardsGranted;
        ProgressHandler.OnProgressChanged += RefreshUI;

        if (unlockNotificationPanel != null) unlockNotificationPanel.SetActive(false);
        if (allCompletePanel != null) allCompletePanel.SetActive(false);

        RefreshUI();
    }

    void OnDestroy()
    {
        ProgressHandler.OnNewGoalStarted -= OnNewGoalStarted;
        ProgressHandler.OnNextUnlockProgressChanged -= OnNextUnlockProgressChanged;
        ProgressHandler.OnRewardsGranted -= OnRewardsGranted;
        ProgressHandler.OnProgressChanged -= RefreshUI;
    }

    void OnNewGoalStarted(Goal goal) => UpdateGoalDisplay(goal);
    void OnNextUnlockProgressChanged(NextUnlockInfo info) => UpdateNextUnlockDisplay(info);

    void OnRewardsGranted(List<Unlockable> rewards)
    {
        if (rewards.Count > 0) ShowUnlockNotification(rewards[0]);
    }

    void RefreshUI()
    {
        if (ProgressHandler.Instance == null) return;

        UpdateGoalDisplay(ProgressHandler.Instance.GetCurrentGoal());
        UpdateNextUnlockDisplay(ProgressHandler.Instance.GetNextUnlock());

        if (ProgressHandler.Instance.IsAllComplete())
        {
            if (allCompletePanel != null) allCompletePanel.SetActive(true);
            if (nextUnlockPanel != null) nextUnlockPanel.SetActive(false);
        }
    }

    void UpdateGoalDisplay(Goal goal)
    {
        if (goal != null)
        {
            if (goalPanel != null) goalPanel.SetActive(true);
            if (goalText != null) goalText.text = goal.displayText;
        }
        else if (goalPanel != null) goalPanel.SetActive(false);
    }

    void UpdateNextUnlockDisplay(NextUnlockInfo info)
    {
        if (info == null || info.unlockable == null)
        {
            if (nextUnlockPanel != null) nextUnlockPanel.SetActive(false);
            return;
        }

        if (nextUnlockPanel != null) nextUnlockPanel.SetActive(true);
        if (progressText != null) progressText.text = info.GetProgressText();

        float progress = info.GetProgressNormalized();
        if (progressSlider != null) progressSlider.value = progress;

        if (unlockIconSilhouette != null && info.unlockable.icon != null)
            unlockIconSilhouette.sprite = info.unlockable.icon;
    }

    void ShowUnlockNotification(Unlockable unlockable)
    {
        if (unlockNotificationPanel == null) return;

        if (unlockNotificationText != null)
            unlockNotificationText.text = $"{unlockable.displayName} Unlocked!";

        if (unlockNotificationIcon != null && unlockable.icon != null)
            unlockNotificationIcon.sprite = unlockable.icon;

        unlockNotificationPanel.SetActive(true);
        StartCoroutine(HideNotificationAfterDelay());
    }

    IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        if (unlockNotificationPanel != null) unlockNotificationPanel.SetActive(false);
    }
}