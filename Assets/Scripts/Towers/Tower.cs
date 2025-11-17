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
    public bool towerAlreadyActivatedThisBeat;

    //tower effects
    public GroundTile tile;
    //used for directing pulses for mono/duo towers
    public List<int> directions;

    internal virtual void Start()
    {
        goalTime = TempoHandler.startDSPTime + TempoHandler.barLength;
        sourceToggle = 0;
        directions = new List<int>();
    }

    internal virtual void Update()
    {
        if (AudioSettings.dspTime > goalTime)
        {
            towerAlreadyActivatedThisBeat = false;
        }
    }

    //Schedules play for the audio clip for this tower, toggling between audio sources
    internal virtual void PlayScheduledClip()
    {
        _audioSources[sourceToggle].clip = playbackClip;
        _audioSources[sourceToggle].PlayScheduled(goalTime);

        sourceToggle = 1 - sourceToggle;
    }

    // Called when a pulse hits this tower
    internal virtual void OnPulseReceived(Pulse incomingPulse)
    {
        // Base implementation does nothing
        // Override in child classes to implement specific behavior
    }

    // Helper method to set a single direction
    public void SetDirection(int direction)
    {
        directions.Clear();
        directions.Add(direction);
    }

    // Helper method to add a direction
    public void AddDirection(int direction)
    {
        if (!directions.Contains(direction))
        {
            directions.Add(direction);
        }
    }
}

public enum TowerType
{
    Source,
    Mono
}
