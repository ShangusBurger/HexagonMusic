using System;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public static List<Tower> allTowers;

    /// <summary>
    /// When true, InteractionMade() does nothing. Set by SaveManager
    /// during map loading so placed towers don't inflate stats.
    /// </summary>
    public static bool SuppressInteractions = false;

    //tempo-related
    public double goalTime;

    //audio playback
    public AudioSource[] _audioSources;
    public AudioClip playbackClip;
    internal int sourceToggle;
    public bool towerAlreadyActivatedThisBeat;
    internal bool isMuted = false;
    internal GameObject visualModel;

    //tower effects
    public GroundTile tile;
    public TowerType ownType;

    //used for directing pulses for mono/splitter/lobber/switcher towers
    public List<int> directions;

    //Tower UI reference
    public TowerUI towerUI;

    public static Action OnInteractionMade; // Event for when the tower is placed

    internal virtual void Start()
    {
        goalTime = TempoHandler.startDSPTime + TempoHandler.barLength;
        sourceToggle = 0;
        towerAlreadyActivatedThisBeat = false;
        visualModel = gameObject.GetComponentInChildren<MeshRenderer>().gameObject;

        if (directions == null || directions.Count == 0)
            directions = new List<int>();

        if (towerUI != null)
        {
            towerUI.SetTargetTower(this);
            towerUI.InitializeDropdown();
        }

        ClearFieldController.OnClearField += DestroySelf;

        if (allTowers == null)
            allTowers = new List<Tower>();

        allTowers.Add(this);

        // Source towers shouldn't count as player interactions
        if (ownType != TowerType.Source)
            InteractionMade();
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

    public virtual void AnimatePulse(int direction)
    {
        if (GetComponent<Animator>() != null)
        {
            Animator anim = GetComponent<Animator>();
            anim.SetTrigger("Pulse");
        }
    }

    public void DestroySelf()
    {
        InteractionMade();
        towerUI.RemoveFromReference(); // This broke
        tile.RemoveTower();
        allTowers.Remove(this);
        ClearFieldController.OnClearField -= DestroySelf;
    }

    //Called when a tower is manually placed or deleted, for tracking interaction goal
    public static void InteractionMade()
    {
        if (SuppressInteractions) return;
        OnInteractionMade?.Invoke();
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
    Buffer,
    Switcher,
    Passer,
    Mirror
}
