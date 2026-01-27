using System;
using System.Collections.Generic;
using UnityEngine;

public class UnlockManager : MonoBehaviour
{
    public static UnlockManager Instance { get; private set; }

    public static event Action<TowerType> OnTowerUnlocked;
    public static event Action<string> OnSampleUnlocked;
    public static event Action<string> OnFeatureUnlocked;
    public static event Action OnUnlocksChanged;

    private HashSet<TowerType> unlockedTowers = new HashSet<TowerType>();
    private HashSet<string> unlockedSamples = new HashSet<string>();
    private HashSet<string> unlockedFeatures = new HashSet<string>();

    [Header("Starting Unlocks - Towers")]
    [SerializeField] private List<TowerType> initialTowers = new List<TowerType> { TowerType.Source, TowerType.Mono };
    
    [Header("Starting Unlocks - Samples (by name)")]
    [SerializeField] private List<string> initialSamples = new List<string> { "Hi-Hat", "Kick", "Snare"};
    
    [Header("Starting Unlocks - Features")]
    [SerializeField] private List<string> initialFeatures = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUnlocks();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeUnlocks()
    {
        foreach (var tower in initialTowers) unlockedTowers.Add(tower);
        foreach (var sample in initialSamples) unlockedSamples.Add(sample);
        foreach (var feature in initialFeatures) unlockedFeatures.Add(feature);
    }

    public void UnlockTower(TowerType tower)
    {
        if (unlockedTowers.Add(tower))
        {
            OnTowerUnlocked?.Invoke(tower);
            OnUnlocksChanged?.Invoke();
        }
    }

    public bool IsTowerUnlocked(TowerType tower) => unlockedTowers.Contains(tower);
    public List<TowerType> GetUnlockedTowers() => new List<TowerType>(unlockedTowers);

    public void UnlockSample(string sampleName)
    {
        if (unlockedSamples.Add(sampleName))
        {
            Debug.Log($"Unlocked sample: {sampleName}");
            OnSampleUnlocked?.Invoke(sampleName);
            OnUnlocksChanged?.Invoke();
        }
    }

    public bool IsSampleUnlocked(string sampleName) => unlockedSamples.Contains(sampleName);
    public List<string> GetUnlockedSamples() => new List<string>(unlockedSamples);

    public void UnlockFeature(string featureId)
    {
        if (unlockedFeatures.Add(featureId))
        {
            Debug.Log($"Unlocked feature: {featureId}");
            OnFeatureUnlocked?.Invoke(featureId);
            OnUnlocksChanged?.Invoke();
        }
    }

    public bool IsFeatureUnlocked(string featureId) => unlockedFeatures.Contains(featureId);

    public void ResetToInitial()
    {
        unlockedTowers.Clear();
        unlockedSamples.Clear();
        unlockedFeatures.Clear();
        InitializeUnlocks();
        OnUnlocksChanged?.Invoke();
    }
}