using System;
using System.Collections.Generic;
using UnityEngine;

public class ProgressHandler : MonoBehaviour
{
    public static ProgressHandler Instance;

    [Header("Current Progress")]
    public int currentLevel = 0;

    [Header("Goals (in order)")]
    [SerializeField] private List<Goal> goalsList = new List<Goal>();

    private Goal currentGoal;
    private NextUnlockInfo cachedNextUnlock;
    private int lastUnlockGoalIndex = -1;

    public static event Action<Goal> OnGoalCompleted;
    public static event Action<Goal> OnNewGoalStarted;
    public static event Action<List<Unlockable>> OnRewardsGranted;
    public static event Action<NextUnlockInfo> OnNextUnlockProgressChanged;
    public static event Action OnProgressChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            CacheNextUnlock();
        }
        else Destroy(gameObject);

        GroundTile.OnTowerChangeMade += ResetCurrentGoal;
    }

    void Update()
    {
        if (currentGoal == null && currentLevel < goalsList.Count)
            SetCurrentGoal(goalsList[currentLevel]);

        if (currentGoal != null && currentGoal.IsComplete())
            CompleteCurrentGoal();

        if (Input.GetKeyDown(KeyCode.P))
            SkipCurrentGoal();
    }

    void ResetCurrentGoal()
    {
        if (currentGoal != null)
            currentGoal.SetupGoal();
    }


    void SetCurrentGoal(Goal goal)
    {
        currentGoal = goal;
        currentGoal.SetupGoal();
        OnNewGoalStarted?.Invoke(currentGoal);
        OnProgressChanged?.Invoke();
    }

    void CompleteCurrentGoal()
    {
        List<Unlockable> grantedRewards = new List<Unlockable>();
        if (currentGoal.HasRewards())
            foreach (var reward in currentGoal.rewards)
                if (reward != null) grantedRewards.Add(reward);
        
        currentGoal.GrantRewards();
        
        if (grantedRewards.Count > 0)
        {
            lastUnlockGoalIndex = currentLevel;
            OnRewardsGranted?.Invoke(grantedRewards);
        }
        
        OnGoalCompleted?.Invoke(currentGoal);
        
        currentLevel++;
        currentGoal = null;

        CacheNextUnlock();
        OnNextUnlockProgressChanged?.Invoke(cachedNextUnlock);
        OnProgressChanged?.Invoke();

        if (currentLevel < goalsList.Count)
            SetCurrentGoal(goalsList[currentLevel]);
    }

    void CacheNextUnlock()
    {
        cachedNextUnlock = null;

        for (int i = currentLevel; i < goalsList.Count; i++)
        {
            Goal goal = goalsList[i];
            if (goal.HasRewards())
            {
                Unlockable firstReward = null;
                foreach (var reward in goal.rewards)
                    if (reward != null) { firstReward = reward; break; }

                if (firstReward != null)
                {
                    cachedNextUnlock = new NextUnlockInfo
                    {
                        unlockable = firstReward,
                        sourceGoal = goal,
                        goalIndex = i,
                        goalsRemaining = i - currentLevel + 1,
                        goalsCompleted = Mathf.Max(0, currentLevel - (lastUnlockGoalIndex + 1))
                    };
                    break;
                }
            }
        }
    }

    public Goal GetCurrentGoal() => currentGoal;
    public int GetCurrentLevel() => currentLevel;
    public int GetTotalGoalCount() => goalsList.Count;
    public NextUnlockInfo GetNextUnlock() => cachedNextUnlock;
    public bool HasNextUnlock() => cachedNextUnlock != null;
    public bool IsAllComplete() => currentLevel >= goalsList.Count;

    public void SkipCurrentGoal()
    {
        if (currentGoal != null)
        {
            currentGoal.DeconstructGoal();
            CompleteCurrentGoal();
        }
    }

    public void ResetProgress()
    {
        if (currentGoal != null) currentGoal.DeconstructGoal();
        currentLevel = 0;
        currentGoal = null;
        lastUnlockGoalIndex = -1;
        cachedNextUnlock = null;
        CacheNextUnlock();
        UnlockManager.Instance?.ResetToInitial();
        OnProgressChanged?.Invoke();
    }
}