using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceOrDeleteTowersGoal", menuName = "Goals/PlaceOrDeleteTowers")]
public class PlaceOrDeleteTowers : Goal
{
    [Header("Goal Settings")]
    public int requiredInteractionCount = 5;
    private int currentInteractionCount = 0;

    public override void SetupGoal()
    {
        currentInteractionCount = 0;
        Tower.OnInteractionMade += IncrementTowerCount;
    }

    public override void DeconstructGoal()
    {
        Tower.OnInteractionMade -= IncrementTowerCount;
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
    private void IncrementTowerCount()
    {
        currentInteractionCount += 1;
        ProgressHandler.UpdateTrackUI(); // Notify the ProgressHandler to update the UI for this track
    }
}