using System;

[Serializable]
public class TrackProgressState
{
    public ProgressionTrack track;
    public int currentLevel;
    public Goal currentGoal;
    public NextUnlockInfo cachedNextUnlock;
    public int lastUnlockGoalIndex = -1;

    public TrackProgressState(ProgressionTrack track)
    {
        this.track = track;
        currentLevel = 0;
        currentGoal = null;
        cachedNextUnlock = null;
        lastUnlockGoalIndex = -1;
    }

    public bool IsComplete => currentLevel >= track.goals.Count;
    public int TotalGoalCount => track.goals.Count;
}