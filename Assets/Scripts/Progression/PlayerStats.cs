using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Tracks lifetime player statistics. Subscribes to the same events
/// used by goals but maintains running totals that never reset.
/// Attach to a persistent GameObject (e.g. the same one as ProgressHandler).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    public int TotalTowerInteractions { get; private set; }
    public int TotalSoundChanges { get; private set; }

    public static event Action OnStatsChanged;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        Tower.OnInteractionMade += OnTowerInteraction;
        TowerUI.OnSampleInteractionMade += OnSoundChange;
    }

    void OnDisable()
    {
        Tower.OnInteractionMade -= OnTowerInteraction;
        TowerUI.OnSampleInteractionMade -= OnSoundChange;
    }

    void OnTowerInteraction()
    {
        TotalTowerInteractions++;
        OnStatsChanged?.Invoke();
    }

    void OnSoundChange()
    {
        TotalSoundChanges++;
        OnStatsChanged?.Invoke();
    }

    public void ResetStats()
    {
        TotalTowerInteractions = 0;
        TotalSoundChanges = 0;
        OnStatsChanged?.Invoke();
    }
}