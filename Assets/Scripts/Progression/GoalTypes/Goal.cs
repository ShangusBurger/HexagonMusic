using System.Collections.Generic;
using UnityEngine;

public class Goal : ScriptableObject
{
    [Header("Goal Info")]
    public string displayText;
    
    [Header("Rewards")]
    [Tooltip("Content to unlock when this goal is completed")]
    public List<Unlockable> rewards = new List<Unlockable>();

    [Header("Display")]
    public Sprite goalIcon;

    public virtual void SetupGoal() { }
    public virtual void DeconstructGoal() { }
    public virtual bool IsComplete() => false;

    public virtual void GrantRewards()
    {
        foreach (var reward in rewards)
            if (reward != null) reward.Unlock();
    }

    public bool HasRewards() => rewards != null && rewards.Count > 0;
}