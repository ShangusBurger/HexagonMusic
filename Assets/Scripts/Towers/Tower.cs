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
        
    }

    void OnEnable()
    {
        GroundTile.OnTowerUpdated += UpdateAudioSignalEffects;
    }

    internal virtual void ScheduleBeat()
    {
        PlayScheduledClip();
        foreach (int direction in tile.pulses)
        {
            Coordinates.Instance.GetNeighbor(tile.tileCoordinate, direction, 1).go.GetComponent<GroundTile>().SchedulePulse(direction);
        }
    }

    //Schedules play for the audio clip for this tower, toggling between audio sources
    internal virtual void PlayScheduledClip()
    {
        _audioSources[sourceToggle].clip = playbackClip;
        _audioSources[sourceToggle].PlayScheduled(goalTime);

        sourceToggle = 1 - sourceToggle;
    }

    internal virtual void UpdateAudioSignalEffects(Coordinate updatedCoordinate)
    {

    }
    
    internal void TriggerDependentTowers()
    {
        foreach (var (tower, distance) in affectedTowers)
        {
            tower.goalTime = this.goalTime + ((double)distance * TempoHandler.beatLength);
            tower.ScheduleBeat();
        }
    }
}
