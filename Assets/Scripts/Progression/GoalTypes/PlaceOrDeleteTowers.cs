using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceOrDeleteTowersGoal", menuName = "Goals/PlaceOrDeleteTowers")]
public class PlaceOrDeleteTowers : Goal
{
    [Header("Goal Settings")]
    public int requiredInteractionCount = 5;

    public override void SetupGoal()
    {
        // No subscription needed — we read directly from lifetime stats
        displayText = "Place, Delete, or Move " + requiredInteractionCount + " Towers to Unlock";
    }

    public override void DeconstructGoal()
    {
        // Nothing to unsubscribe
    }

    public override bool IsComplete()
    {
        if (PlayerStats.Instance == null) return false;
        return PlayerStats.Instance.TotalTowerInteractions >= requiredInteractionCount;
    }

    public override float GetProgressNormalized()
    {
        if (PlayerStats.Instance == null || requiredInteractionCount <= 0) return 0f;
        return Mathf.Clamp01((float)PlayerStats.Instance.TotalTowerInteractions / requiredInteractionCount);
    }

    public override string GetProgressText()
    {
        int current = PlayerStats.Instance != null ? PlayerStats.Instance.TotalTowerInteractions : 0;
        return $"{current}/{requiredInteractionCount}";
    }
}