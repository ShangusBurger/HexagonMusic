using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/UseNumberSamples")]
public class UseNumberSamples : Goal
{
    public int numberOfSamplesNeeded;

    public override void SetupGoal()
    {
        
    }

    public override void DeconstructGoal()
    {
       
    }

    public override bool IsComplete()
    {
        List<AudioClip> samplesUsed = new List<AudioClip>();

        foreach (Tower t in Tower.allTowers)
        {
            if (!samplesUsed.Contains(t.playbackClip))
            {
                samplesUsed.Add(t.playbackClip);
            }
        }

        if (samplesUsed.Count >= numberOfSamplesNeeded)
        {
            return true;
        }

        return false;
    }
}