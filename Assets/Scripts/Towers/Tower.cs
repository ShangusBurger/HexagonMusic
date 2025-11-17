using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class Tower : MonoBehaviour
{
    //tempo-related
    public double goalTime;

    //audio playback
    public AudioSource[] _audioSources;
    public AudioClip playbackClip;
    private int sourceToggle;
    public bool towerActivatedThisBeat;

    //tower effects
    public GroundTile tile;
    public List<(Tower, int)> affectedTowers = new List<(Tower, int)>();

    internal virtual void Start()
    {
        goalTime = TempoHandler.startDSPTime + TempoHandler.barLength;
        sourceToggle = 0;

        UpdateAudioSignalEffects(null);
    }

    internal virtual void Update()
    {
        if (AudioSettings.dspTime > goalTime)
        {
            towerActivatedThisBeat = false;
        }
    }

    void OnEnable()
    {
        GroundTile.OnTowerUpdated += UpdateAudioSignalEffects;
    }

    //Schedules play for the audio clip for this tower, toggling between audio sources
    internal virtual void PlayScheduledClip()
    {
        _audioSources[sourceToggle].clip = playbackClip;
        _audioSources[sourceToggle].PlayScheduled(goalTime);

        sourceToggle = 1 - sourceToggle;
    }

    internal virtual void OfferSetTower() { 
        
    }

    internal virtual void UpdateAudioSignalEffects(Coordinate updatedCoordinate)
    {

    }

    /* internal void TriggerDependentTowers()
    {
        foreach (var (tower, distance) in affectedTowers)
        {
            tower.goalTime = this.goalTime + ((double)distance * TempoHandler.beatLength);
            //tower.ScheduleBeat();
        }
    } */

}

public enum TowerType
{
    Source,
    Mono
}
