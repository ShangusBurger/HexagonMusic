using UnityEngine;

[CreateAssetMenu(fileName = "NewChangeSoundSamplesGoal", menuName = "Goals/ChangeSoundSamples")]
public class ChangeSoundSamples : Goal
{
    [Header("Goal Settings")]
    public int requiredInteractionCount = 5;
    private int currentInteractionCount = 0;

    private void Start() {
        currentInteractionCount = 0;
    }

    public override void SetupGoal()
    {
        // No subscription needed — we read directly from lifetime stats
        displayText = "Change The Sound of any Tower " + requiredInteractionCount + " Times to Unlock";
    }

    public override void DeconstructGoal()
    {
        // Nothing to unsubscribe
    }

    public override bool IsComplete()
    {
        if (PlayerStats.Instance == null) return false;
        return PlayerStats.Instance.TotalSoundChanges >= requiredInteractionCount;
    }

    public override float GetProgressNormalized()
    {
        if (PlayerStats.Instance == null || requiredInteractionCount <= 0) return 0f;
        return Mathf.Clamp01((float)PlayerStats.Instance.TotalSoundChanges / requiredInteractionCount);
    }

    public override string GetProgressText()
    {
        int current = PlayerStats.Instance != null ? PlayerStats.Instance.TotalSoundChanges : 0;
        return $"{current}/{requiredInteractionCount}";
    }

    // This method is called whenever a tower is placed or removed
    private void IncrementSampleCount()
    {
        currentInteractionCount += 1;
        ProgressHandler.UpdateTrackUI(); // Notify the ProgressHandler to update the UI for this track
    }
}