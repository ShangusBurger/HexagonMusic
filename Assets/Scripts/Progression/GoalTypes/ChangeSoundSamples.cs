using UnityEngine;

[CreateAssetMenu(fileName = "NewChangeSoundSamplesGoal", menuName = "Goals/ChangeSoundSamples")]
public class ChangeSoundSamples : Goal
{
    [Header("Goal Settings")]
    public int requiredInteractionCount = 5;
    private int currentInteractionCount = 0;

    public override void SetupGoal()
    {
        currentInteractionCount = 0;
        TowerUI.OnSampleInteractionMade += IncrementSampleCount;
    }

    public override void DeconstructGoal()
    {
        TowerUI.OnSampleInteractionMade -= IncrementSampleCount;
    }

    public override bool IsComplete()
    {
        return currentInteractionCount >= requiredInteractionCount
;
    }

    public override float GetProgressNormalized()
    {
        if (requiredInteractionCount <= 0) return 1f;
        return Mathf.Clamp01((float)currentInteractionCount / requiredInteractionCount);
    }

    public override string GetProgressText()
    {
        return $"{currentInteractionCount}/{requiredInteractionCount}";
    }

    // This method is called whenever a tower is placed or removed
    private void IncrementSampleCount()
    {
        currentInteractionCount += 1;
        ProgressHandler.UpdateTrackUI(); // Notify the ProgressHandler to update the UI for this track
    }
}