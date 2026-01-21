[System.Serializable]
public class NextUnlockInfo
{
    public Unlockable unlockable;
    public Goal sourceGoal;
    public int goalIndex;
    public int goalsRemaining;
    public int goalsCompleted;

    public float GetProgressNormalized()
    {
        int total = goalsCompleted + goalsRemaining;
        return total <= 0 ? 1f : (float)goalsCompleted / total;
    }

    public string GetProgressText()
    {
        return $"{goalsCompleted}/{goalsCompleted + goalsRemaining}";
    }
}