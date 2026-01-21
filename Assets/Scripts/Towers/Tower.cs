using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    //tempo-related
    public double goalTime;

    //audio playback
    public AudioSource[] _audioSources;
    public AudioClip playbackClip;
    internal int sourceToggle;
    public bool towerAlreadyActivatedThisBeat;
    internal LibPdInstance pdInstance;
    internal bool isMuted = false;
    internal GameObject visualModel;

    //tower effects
    public GroundTile tile;

    //used for directing pulses for mono/duo/lobber towers
    public List<int> directions;

    //Tower UI reference
    public TowerUI towerUI;

    internal virtual void Start()
    {
        goalTime = TempoHandler.startDSPTime + TempoHandler.barLength;
        sourceToggle = 0;
        directions = new List<int>();
        towerAlreadyActivatedThisBeat = false;
        visualModel = gameObject.GetComponentInChildren<MeshRenderer>().gameObject;
        {
            pdInstance = GetComponent<LibPdInstance>();
        }

        if (towerUI != null)
        {
            towerUI.SetTargetTower(this);
            towerUI.InitializeDropdown();
        }

        ClearFieldController.OnClearField += DestroySelf;
    }

    internal virtual void Update()
    {
        if (AudioSettings.dspTime > goalTime)
        {
            towerAlreadyActivatedThisBeat = false;
        }
    }

    //Schedules play for the audio clip (and resets goal time) for this tower, toggling between audio sources
    internal virtual void PlayScheduledClip()
    {
        towerAlreadyActivatedThisBeat = true;
        
        if (!isMuted)
        {
            _audioSources[sourceToggle].clip = playbackClip;
            _audioSources[sourceToggle].PlayScheduled(goalTime);
        }

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

    public void ToggleMute()
    {
        isMuted = !isMuted;

        if (isMuted)
        {
            towerUI.muteButtonImage.sprite = towerUI.mutedSprite;
        }
        else
        {
            towerUI.muteButtonImage.sprite = towerUI.unmutedSprite;
        }
    }

    public virtual void SetSelfUI()
    {
        return;
    }

    public void DestroySelf()
    {
        towerUI.RemoveFromReference();
        tile.RemoveTower();
    }
}

public enum TowerType
{
    Source,
    Mono,
    Splitter,
    Sink,
    Lobber,
    Sprayer,
    Buffer
}
