using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/UseNumberSamples")]
public class UseNumberSamples : Goal
{
    public int numberOfSamplesNeeded;

    public override void SetupGoal() { }

    public override void DeconstructGoal() { }

    private int GetUniqueSampleCount()
    {
        HashSet<AudioClip> samplesUsed = new HashSet<AudioClip>();
        foreach (Tower t in Tower.allTowers)
        {
            if (t.playbackClip != null)
                samplesUsed.Add(t.playbackClip);
        }
        return samplesUsed.Count;
    }

    public override bool IsComplete()
    {
        Debug.Log($"Samples used: {GetUniqueSampleCount()}");
        return GetUniqueSampleCount() >= numberOfSamplesNeeded;
    }

    public override float GetProgressNormalized()
    {
        if (numberOfSamplesNeeded <= 0) return 1f;
        return Mathf.Clamp01((float)GetUniqueSampleCount() / numberOfSamplesNeeded);
    }

    public override string GetProgressText()
    {
        return $"{GetUniqueSampleCount()}/{numberOfSamplesNeeded}";
    }
}