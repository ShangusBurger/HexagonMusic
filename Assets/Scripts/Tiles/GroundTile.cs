using System;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class GroundTile : MonoBehaviour
{
    //Selection Colors
    [SerializeField] private Color highlightMaterialColor;
    [SerializeField] private Color lowlightMaterialColor;
    [SerializeField] private Color selectedMaterialColor;
    [SerializeField] private Color beatMaterialColor;
    [SerializeField] private Color goalCompleteColor;
    private Renderer tileRenderer;

    // original color, updated when goal is set to tile
    [SerializeField] private Color originalColor;
    
    // default to white always
    [SerializeField] private Color defaultColor;

    //Tile Contents and Identity
    public Coordinate tileCoordinate;
    public Tower tower;
    public List<Pulse> pulses; //List of directions toward which the signal is flowing on the next pulse of the beat
    public List<Pulse> pulsesCached; //List of pulses to be processed on the next update
    public int beatsUntilPulse = -1;
    public static event Action PulseExistsNotif;
    public bool isGoalTile = false;
    public bool goalTriggered = false;


    //Handling Updates
    private bool triggerBeatNextUpdate = false;
    public static event Action<Coordinate> OnTowerUpdated;
    public double visualDelay = 0.0;
    public static event Action OnTowerChangeMade;

    // Fading variables
    [SerializeField] private float fadeDuration = 1f; // Duration of fade in seconds
    private bool isFading = false;
    private float fadeTimer = 0f;
    private Color fadeStartColor;
    private Color fadeTargetColor;

    // Instantiates Lists and sets variables when tilemap is created
    void Start()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
        beatsUntilPulse = -1;
        pulses = new List<Pulse>();
        pulsesCached = new List<Pulse>();

        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
            defaultColor = originalColor;
        }
    }

    // Subscribe to beat event in TempoHandler
    void OnEnable()
    {
        TempoHandler.TriggerBeat += BeatRecieved;
    }

    void Update()
    {
        if (pulsesCached.Count > 0 || pulses.Count > 0)
        {
            PulseExistsNotif?.Invoke();
            if (isGoalTile)
            {
                foreach (Pulse p in pulses)
                {
                    if (!p.source)
                    {
                        goalTriggered = true;
                        tileRenderer.material.color = goalCompleteColor;
                        originalColor = goalCompleteColor;
                    }
                }
            }
        }

        

        visualDelay -= (double) Time.deltaTime;
        // Handle color fading
        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);
            
            if (SelectionHandler.currentSelectedTile != this && SelectionHandler.currentHoveredTile != this)
            {
                tileRenderer.material.color = Color.Lerp(fadeStartColor, fadeTargetColor, t);
            }

            if (t >= 1f)
            {
                isFading = false;
            }
        }

        // if pulse happened last beat, propagate pulse to next tiles on next frame
        if (pulsesCached.Count != 0 && visualDelay <= 0.0)
        {
            //for each pulse cached, either return it to the list with delay, or send it to next tile
            foreach (Pulse pulse in pulsesCached)
            {
                if (pulse.delay > 0 && pulse.life != 0)
                {
                    pulse.delay -= 1;
                    pulse.life--;
    
                    //if delay is now 0 and a tower exists on the tile, play tower sound and notify tower of pulse, otherwise re-add to pulse list to propigate
                    if (pulse.delay == 0 && tower != null)
                    {
                        if (!tower.towerAlreadyActivatedThisBeat)
                        {
                            tower.PlayScheduledClip();
                        }
                        if (!pulse.source)
                        {
                            tower.OnPulseReceived(pulse);
                        }
                    }
                    else
                    {
                        pulses.Add(pulse);
                    }
                    continue;
                }
                
                StartFade(beatMaterialColor, originalColor, pulse.direction);
                
                //whether or not the pulse actually continues to another tile is handled in PropagatePulse()
                PropagatePulse(pulse);
            }
            pulsesCached.Clear();
        }
        
        //when a beat is triggered, add pulses to cache to be handled next tick, return normal state of tile
        if (triggerBeatNextUpdate)
        {
            triggerBeatNextUpdate = false;
            
        }
    }

    private void StartFade(Color from, Color to, int direction)
    {
        if (SelectionHandler.currentSelectedTile != this && SelectionHandler.currentHoveredTile != this)
        {
            tileRenderer.material.color = from;
        }
        
        fadeStartColor = from;
        fadeTargetColor = to;
        fadeTimer = 0f;
        isFading = true;

        if (tower != null)
        {
            tower.AnimatePulse(direction);
        }
    }

    // used to create a new Pulse, either on this tile or to propagate to another tile
    public void SchedulePulse(Pulse pulse)
    {
        foreach (Pulse p in pulses)
        {
            if (p.direction == pulse.direction && p.delay == pulse.delay && !pulse.source)
                return;
        }

        pulse.originTile = tileCoordinate;
        pulses.Add(pulse);

        //play tower sound and notify existing tower of pulse
        if (tower != null && pulse.delay <= 0)
        {
            pulse.continuous = false;
            // Notify the tower that a pulse has been received
            if (!pulse.source)
            {
                tower.OnPulseReceived(pulse);
            }

            if (!tower.towerAlreadyActivatedThisBeat)
            {
                tower.PlayScheduledClip();
            }
            
        }
    }

    public void Highlight()
    {

        tileRenderer.material.color = highlightMaterialColor;

    }
    public void Lowlight()
    {
        tileRenderer.material.color = lowlightMaterialColor;
    }

    //Called from SelectionHandler when this tile is selected
    public void Select()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = selectedMaterialColor;
        }

        if (tower == null)
        {
            SelectionHandler.OfferTowerPlacement(this);
            OnTowerUpdated?.Invoke(TileMapConstructor.allTiles.GetCoordinateFromWorldPosition(transform.position));
            return;
        }
        SelectionHandler.HideTowerUIs();
        SelectionHandler.OpenTowerUI(this);
    }

    public void Deselect()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = originalColor;
        }
    }

    public void BeatRecieved()
    {
        visualDelay = TempoHandler.beatLength * 3/4;

            //if active pulse on tile, add to cache to be sent onward next update
            if (pulses.Count > 0)
            {  
                foreach (Pulse p in pulses)
                {
                    pulsesCached.Add(p);
                }
                pulses.Clear();
            }
    }

    public void PropagatePulse(Pulse pulse)
    {
        if (Coordinates.Instance.GetNeighbor(tileCoordinate, pulse.direction, 1) != null && (pulse.continuous || pulse.source) && pulse.life != 0)
        {
            if (pulse.life > 0)
            {
                pulse.life--;
            }
            Pulse nextPulse = new Pulse(pulse.direction, life: pulse.life);
            Coordinates.Instance.GetNeighbor(tileCoordinate, pulse.direction, 1).go.GetComponent<GroundTile>().SchedulePulse(nextPulse);
        }
    }

    public void RemoveTower()
    {
        if (tower != null)
        {
            Destroy(tower.gameObject);
            tower = null;
            Deselect();
            SelectionHandler.currentSelectedTile = null;
        }
        OnTowerChangeMade?.Invoke();
    }

    public void AddTowerToTile()
    {
        tower = Instantiate(TileMapConstructor.Instance.sourceTowerPrefab, transform).GetComponent<Tower>();
        tower.tile = this;
    }

    public void AddTowerToTile(TowerType type)
    {   
        switch (type)
        {
            case TowerType.Source:
                tower = Instantiate(TileMapConstructor.Instance.sourceTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                break;
            case TowerType.Mono:
                tower = Instantiate(TileMapConstructor.Instance.monoTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                SelectionHandler.currentMouseState = MouseState.SetMonoTower;
                break;
            case TowerType.Splitter:
                tower = Instantiate(TileMapConstructor.Instance.splitterTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                SelectionHandler.DeselectCurrent();
                break;
            case TowerType.Sink:
                tower = Instantiate(TileMapConstructor.Instance.sinkTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                SelectionHandler.DeselectCurrent();
                break;
            case TowerType.Lobber:
                tower = Instantiate(TileMapConstructor.Instance.lobberTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                SelectionHandler.currentMouseState = MouseState.SetLobberTower;
                break;
            case TowerType.Sprayer:
                tower = Instantiate(TileMapConstructor.Instance.sprayerTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                break;
            case TowerType.Buffer:
                tower = Instantiate(TileMapConstructor.Instance.bufferTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                break;
            case TowerType.Switcher:
                tower = Instantiate(TileMapConstructor.Instance.switcherTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                break;
        }
        tower.ownType = type;

        OnTowerChangeMade?.Invoke();
        SelectionHandler.HideTowerUIs();
    }
    public void SetAsGoalTile(Color goalColor)
    {
        tileRenderer.material.color = goalColor;
        originalColor = goalColor;
        fadeTargetColor = goalColor;
        isGoalTile = true;
        goalTriggered = false;
    }

    public void RemoveGoalTile()
    {
        originalColor = defaultColor;
        fadeTargetColor = defaultColor;
        tileRenderer.material.color = defaultColor;
        isGoalTile = false;
        goalTriggered = false;
    }  
}
