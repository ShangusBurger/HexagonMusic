using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProgressHandler : MonoBehaviour
{
    public static ProgressHandler Instance;

    [Header("Progression Tracks")]
    [SerializeField] private List<ProgressionTrack> tracks = new List<ProgressionTrack>();

    private Dictionary<string, TrackProgressState> trackStates = new Dictionary<string, TrackProgressState>();

    // Events now include track ID for identification
    public static event Action<string, Goal> OnGoalCompleted;          // trackId, goal
    public static event Action<string, Goal> OnNewGoalStarted;         // trackId, goal
    public static event Action<string, List<Unlockable>> OnRewardsGranted;  // trackId, rewards
    public static event Action<string, NextUnlockInfo> OnNextUnlockProgressChanged;  // trackId, info
    public static event Action<string> OnTrackProgressChanged;         // trackId
    public static event Action<string> OnTrackCompleted;               // trackId
    public static event Action OnAnyProgressChanged;                   // fired when any track changes

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTracks();
        }
        else Destroy(gameObject);

    }

    void OnDestroy()
    {

    }

    void InitializeTracks()
    {
        trackStates.Clear();
        foreach (var track in tracks)
        {
            if (track != null && !string.IsNullOrEmpty(track.trackId))
            {
                var state = new TrackProgressState(track);
                trackStates[track.trackId] = state;
                CacheNextUnlock(state);
            }
        }
    }

    void Update()
    {
        foreach (var state in trackStates.Values)
        {
            if (state.currentGoal == null && state.currentLevel < state.track.goals.Count)
                SetCurrentGoal(state, state.track.goals[state.currentLevel]);

            if (state.currentGoal != null && state.currentGoal.IsComplete())
                CompleteCurrentGoal(state);
        }

        // Debug: Skip goals with P key (track 0) and O key (track 1)
        if (Input.GetKeyDown(KeyCode.P)) SkipCurrentGoal(0);
        if (Input.GetKeyDown(KeyCode.O)) SkipCurrentGoal(1);
    }

    void SetCurrentGoal(TrackProgressState state, Goal goal)
    {
        state.currentGoal = goal;
        state.currentGoal.SetupGoal();
        OnNewGoalStarted?.Invoke(state.track.trackId, state.currentGoal);
        OnTrackProgressChanged?.Invoke(state.track.trackId);
        OnAnyProgressChanged?.Invoke();
    }

    void CompleteCurrentGoal(TrackProgressState state)
    {
        var grantedRewards = new List<Unlockable>();
        if (state.currentGoal.HasRewards())
            foreach (var reward in state.currentGoal.rewards)
                if (reward != null) grantedRewards.Add(reward);

        state.currentGoal.GrantRewards();

        if (grantedRewards.Count > 0)
        {
            state.lastUnlockGoalIndex = state.currentLevel;
            OnRewardsGranted?.Invoke(state.track.trackId, grantedRewards);
        }

        OnGoalCompleted?.Invoke(state.track.trackId, state.currentGoal);

        state.currentLevel++;
        state.currentGoal = null;

        CacheNextUnlock(state);
        OnNextUnlockProgressChanged?.Invoke(state.track.trackId, state.cachedNextUnlock);
        OnTrackProgressChanged?.Invoke(state.track.trackId);
        OnAnyProgressChanged?.Invoke();

        if (state.IsComplete)
            OnTrackCompleted?.Invoke(state.track.trackId);
        else if (state.currentLevel < state.track.goals.Count)
            SetCurrentGoal(state, state.track.goals[state.currentLevel]);
    }

    void CacheNextUnlock(TrackProgressState state)
    {
        state.cachedNextUnlock = null;

        for (int i = state.currentLevel; i < state.track.goals.Count; i++)
        {
            Goal goal = state.track.goals[i];
            if (goal.HasRewards())
            {
                Unlockable firstReward = null;
                foreach (var reward in goal.rewards)
                    if (reward != null) { firstReward = reward; break; }

                if (firstReward != null)
                {
                    state.cachedNextUnlock = new NextUnlockInfo
                    {
                        unlockable = firstReward,
                        sourceGoal = goal,
                        trackId = state.track.trackId,
                        goalIndex = i,
                        goalsRemaining = i - state.currentLevel + 1,
                        goalsCompleted = Mathf.Max(0, state.currentLevel - (state.lastUnlockGoalIndex + 1))
                    };
                    break;
                }
            }
        }
    }

    // Public API - by track ID
    public TrackProgressState GetTrackState(string trackId) =>
        trackStates.TryGetValue(trackId, out var state) ? state : null;

    public Goal GetCurrentGoal(string trackId) => GetTrackState(trackId)?.currentGoal;
    public int GetCurrentLevel(string trackId) => GetTrackState(trackId)?.currentLevel ?? 0;
    public int GetTotalGoalCount(string trackId) => GetTrackState(trackId)?.TotalGoalCount ?? 0;
    public NextUnlockInfo GetNextUnlock(string trackId) => GetTrackState(trackId)?.cachedNextUnlock;
    public bool IsTrackComplete(string trackId) => GetTrackState(trackId)?.IsComplete ?? true;

    // Public API - by index (convenience for UI)
    public TrackProgressState GetTrackStateByIndex(int index) =>
        index >= 0 && index < tracks.Count ? GetTrackState(tracks[index].trackId) : null;

    public int TrackCount => tracks.Count;
    public List<ProgressionTrack> GetAllTracks() => new List<ProgressionTrack>(tracks);

    public void SkipCurrentGoal(string trackId)
    {
        var state = GetTrackState(trackId);
        if (state?.currentGoal != null)
        {
            state.currentGoal.DeconstructGoal();
            CompleteCurrentGoal(state);
        }
    }

    public void SkipCurrentGoal(int trackIndex)
    {
        var state = GetTrackStateByIndex(trackIndex);
        if (state != null) SkipCurrentGoal(state.track.trackId);
    }

    public void ResetTrack(string trackId)
    {
        var state = GetTrackState(trackId);
        if (state == null) return;

        if (state.currentGoal != null) state.currentGoal.DeconstructGoal();
        state.currentLevel = 0;
        state.currentGoal = null;
        state.lastUnlockGoalIndex = -1;
        state.cachedNextUnlock = null;
        CacheNextUnlock(state);
        OnTrackProgressChanged?.Invoke(trackId);
        OnAnyProgressChanged?.Invoke();
    }

    public void ResetAllProgress()
    {
        foreach (var state in trackStates.Values)
        {
            if (state.currentGoal != null) state.currentGoal.DeconstructGoal();
            state.currentLevel = 0;
            state.currentGoal = null;
            state.lastUnlockGoalIndex = -1;
            state.cachedNextUnlock = null;
            CacheNextUnlock(state);
        }
        UnlockManager.Instance?.ResetToInitial();
        OnAnyProgressChanged?.Invoke();
    }

    public bool AreAllTracksComplete()
    {
        foreach (var state in trackStates.Values)
            if (!state.IsComplete) return false;
        return true;
    }

    public static void UpdateTrackUI()
    {
        foreach (var track in Instance.tracks)
        {
            OnTrackProgressChanged?.Invoke(track.trackId);
        }
        
    }

    public void HideTrackMeter(string trackId)
    {
    }
}