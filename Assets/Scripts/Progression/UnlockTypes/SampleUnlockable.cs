using UnityEngine;

[CreateAssetMenu(fileName = "NewSampleUnlock", menuName = "Unlockables/Sample")]
public class SampleUnlockable : Unlockable
{
    [Header("Sample Settings")]
    [Tooltip("Exact sample name as it appears in SampleLibrary")]
    public string sampleName;

    public override void Unlock()
    {
        if (UnlockManager.Instance != null)
            UnlockManager.Instance.UnlockSample(sampleName);
    }

    public override bool IsUnlocked()
    {
        return UnlockManager.Instance != null && UnlockManager.Instance.IsSampleUnlocked(sampleName);
    }
}