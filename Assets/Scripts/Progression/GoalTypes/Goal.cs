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

    [Header("Gating (optional)")]
    [Tooltip("If set, this goal cannot be started until the specified tower type is unlocked.")]
    public TowerType gatingTowerType = (TowerType)(-1);  // -1 = no gate
    
    /// <summary>
    /// Returns true if this goal has a gating requirement.
    /// </summary>
    public bool HasGate => (int)gatingTowerType >= 0;

    /// <summary>
    /// Returns true if the gating condition is met (or if there is no gate).
    /// </summary>
    public bool IsGateSatisfied()
    {
        if (!HasGate) return true;
        return UnlockManager.Instance != null 
            && UnlockManager.Instance.IsTowerUnlocked(gatingTowerType);
    }

    public virtual bool showProgressUI => true;

    public virtual void SetupGoal() { }
    public virtual void DeconstructGoal() { }
    public virtual bool IsComplete() => false;

    public virtual float GetProgressNormalized() => IsComplete() ? 1f : 0f;
    public virtual string GetProgressText() => "";

    public virtual void GrantRewards()
    {
        foreach (var reward in rewards)
            if (reward != null) reward.Unlock();
    }

    public bool HasRewards() => rewards != null && rewards.Count > 0;
}