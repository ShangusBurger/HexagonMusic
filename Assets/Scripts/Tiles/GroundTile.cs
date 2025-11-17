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
    [SerializeField] private Color selectedMaterialColor;
    [SerializeField] private Color beatMaterialColor;
    private Renderer tileRenderer;
    private Color originalColor;

    //Tile Contents and Identity
    public Coordinate tileCoordinate;
    public Tower tower;
    public List<int> pulses; //List of directions toward which the signal is flowing on the next pulse of the beat
    public List<int> pulsesCached;
    public int beatsUntilPulse = -1;


    //Handling Updates
    private bool triggerBeatNextUpdate = false;
    private bool propigatePulseNextUpdate = false;
    public static event Action<Coordinate> OnTowerUpdated;

    [SerializeField] private GameObject defaultTowerPrefab;

    void Start()
    {
        tileRenderer = GetComponentInChildren<Renderer>();
        beatsUntilPulse = -1;
        pulses = new List<int>();
        pulsesCached = new List<int>();

        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
        }
    }

    void OnEnable()
    {
        TempoHandler.TriggerBeat += BeatRecieved;
    }

    void Update()
    {
        // if pulse happened last frame, propagate pulse to next tiles on next beat
        if (pulsesCached.Count != 0)
        {
            foreach (int direction in pulsesCached)
            {
                PropagatePulse(direction);
            }
            pulsesCached.Clear();
        }
        
        
        if (triggerBeatNextUpdate)
        {
            triggerBeatNextUpdate = false;

            //if active pulse on tile, do stuff, add to cache to be sent onward next update
            if (pulses.Count > 0)
            {
                foreach (int direction in pulses)
                {
                    if (tower == null)
                        pulsesCached.Add(direction);
                }
                pulses.Clear();

                if (SelectionHandler.currentSelectedTile != this)
                    tileRenderer.material.color = beatMaterialColor;
                return;
            }

            if (tileRenderer.material.color == beatMaterialColor && SelectionHandler.currentSelectedTile != this)
            {
                tileRenderer.material.color = originalColor;
            }

        }
    }

    public void SchedulePulse(int direction)
    {
        pulses.Add(direction);
    }

    public void Highlight()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = highlightMaterialColor;
        }
    }
    public void Select()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = selectedMaterialColor;
        }

        if (tower == null && defaultTowerPrefab != null)
        {
            AddTowerToTile();
            OnTowerUpdated?.Invoke(TileMapConstructor.allTiles.GetCoordinateFromWorldPosition(transform.position));
        }
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

    void PropagatePulse(int direction)
    {
        if (Coordinates.Instance.GetNeighbor(tileCoordinate, direction, 1) != null)
        {
            Coordinates.Instance.GetNeighbor(tileCoordinate, direction, 1).go.GetComponent<GroundTile>().SchedulePulse(direction);
        }
    }

    public void Remove()
    {
        if (tower != null)
        {
            Destroy(tower.gameObject);
            tower = null;
        }
    }

    public void AddTowerToTile()
    {
        tower = Instantiate(defaultTowerPrefab, transform).GetComponent<Tower>();
        tower.tile = this;
    }

    public void AddTowerToTile(GameObject towerPrefab)
    {
        tower = Instantiate(towerPrefab, transform).GetComponent<Tower>();
        tower.tile = this;
    }

}
