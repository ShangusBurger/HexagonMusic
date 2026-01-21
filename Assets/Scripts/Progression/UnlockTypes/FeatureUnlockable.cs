using UnityEngine;

[CreateAssetMenu(fileName = "NewFeatureUnlock", menuName = "Unlockables/Feature")]
public class FeatureUnlockable : Unlockable
{
    [Header("Feature Settings")]
    public string featureId;

    public override void Unlock()
    {
        if (UnlockManager.Instance != null)
            UnlockManager.Instance.UnlockFeature(featureId);
    }

    public override bool IsUnlocked()
    {
        return UnlockManager.Instance != null && UnlockManager.Instance.IsFeatureUnlocked(featureId);
    }
}