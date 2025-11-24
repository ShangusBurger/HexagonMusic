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
    private Renderer tileRenderer;
    private Color originalColor;

    //Tile Contents and Identity
    public Coordinate tileCoordinate;
    public Tower tower;
    public List<Pulse> pulses; //List of directions toward which the signal is flowing on the next pulse of the beat
    public List<Pulse> pulsesCached; //List of pulses to be processed on the next update
    public int beatsUntilPulse = -1;


    //Handling Updates
    private bool triggerBeatNextUpdate = false;
    public static event Action<Coordinate> OnTowerUpdated;

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
        }
    }

    // Subscribe to beat event in TempoHandler
    void OnEnable()
    {
        TempoHandler.TriggerBeat += BeatRecieved;
    }

    void Update()
    {
        // Handle color fading
        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);
            
            if (SelectionHandler.currentSelectedTile != this)
            {
                tileRenderer.material.color = Color.Lerp(fadeStartColor, fadeTargetColor, t);
            }

            if (t >= 1f)
            {
                isFading = false;
            }
        }

        // if pulse happened last frame, propagate pulse to next tiles on next beat
        if (pulsesCached.Count != 0)
        {
            //for each pulse cached, either return it to the list with delay, or send it to next tile
            foreach (Pulse pulse in pulsesCached)
            {
                if (pulse.delay > 0)
                {
                    pulse.delay -= 1;
    
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
                if (SelectionHandler.currentSelectedTile != this)
                    tileRenderer.material.color = beatMaterialColor;
                    StartFade(beatMaterialColor, originalColor);
                
                //whether or not the pulse actually continues to another tile is handled in PropagatePulse()
                PropagatePulse(pulse);
            }
            pulsesCached.Clear();
        }
        
        //when a beat is triggered, add pulses to cache to be handled next tick, return normal state of tile
        if (triggerBeatNextUpdate)
        {
            triggerBeatNextUpdate = false;

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
    }

    private void StartFade(Color from, Color to)
    {
        tileRenderer.material.color = from;
        fadeStartColor = from;
        fadeTargetColor = to;
        fadeTimer = 0f;
        isFading = true;
    }

    // used to create a new Pulse, either on this tile or to propagate to another tile
    public void SchedulePulse(Pulse pulse)
    {
        pulse.originTile = tileCoordinate;
        pulses.Add(pulse);

        //play tower sound (and redirect pulse according to tower rules) if there is a tower on this tile
        if (tower != null && pulse.delay <= 0)
        {
            pulse.continuous = false;
            if (!tower.towerAlreadyActivatedThisBeat)
            {
                tower.PlayScheduledClip();

                // Notify the tower that a pulse has been received
                if (!pulse.source)
                {
                    tower.OnPulseReceived(pulse);
                }
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
        triggerBeatNextUpdate = true;
    }

    public void PropagatePulse(Pulse pulse)
    {
        if (Coordinates.Instance.GetNeighbor(tileCoordinate, pulse.direction, 1) != null && (pulse.continuous || pulse.source))
        {
            Pulse nextPulse = new Pulse(pulse.direction);
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
                break;
            case TowerType.Lobber:
                tower = Instantiate(TileMapConstructor.Instance.lobberTowerPrefab, transform).GetComponent<Tower>();
                tower.tile = this;
                SelectionHandler.currentMouseState = MouseState.SetLobberTower;
                break;
        }
        
    }

}
