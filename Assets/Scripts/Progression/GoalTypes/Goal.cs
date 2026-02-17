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

    /// <summary>
    /// Returns normalized progress (0-1) toward completing this goal.
    /// Override in subclasses to provide meaningful progress tracking.
    /// </summary>
    public virtual float GetProgressNormalized() => IsComplete() ? 1f : 0f;

    /// <summary>
    /// Returns a display string for current progress (e.g., "3/5").
    /// Override in subclasses for custom formatting.
    /// </summary>
    public virtual string GetProgressText() => "";

    public virtual void GrantRewards()
    {
        foreach (var reward in rewards)
            if (reward != null) reward.Unlock();
    }

    public bool HasRewards() => rewards != null && rewards.Count > 0;
}