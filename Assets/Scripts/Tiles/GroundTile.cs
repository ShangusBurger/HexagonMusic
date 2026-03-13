using System;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class GroundTile : MonoBehaviour
{
    // ── Selection Colors ──────────────────────────────────────────────
    [SerializeField] private Color highlightMaterialColor;
    [SerializeField] private Color lowlightMaterialColor;
    [SerializeField] private Color infoLowlightMaterialColor;
    [SerializeField] private Color selectedMaterialColor;
    [SerializeField] private Color activeBeatMaterialColor;
    [SerializeField] private Color beatMaterialColor;
    [SerializeField] private Color goalCompleteColor;
    private Renderer tileRenderer;

    // original color, updated when goal is set to tile
    [SerializeField] private Color originalColor;

    // default to white always
    [SerializeField] private Color defaultColor;

    // ── Lowlight persistence flags ────────────────────────────────────
    private bool isLowlighted = false;
    private bool isInfoLowlighted = false;

    // ── Tile Contents and Identity ────────────────────────────────────
    public Coordinate tileCoordinate;
    public Tower tower;
    public List<Pulse> pulses;
    public List<Pulse> pulsesCached;
    public int beatsUntilPulse = -1;
    public static event Action PulseExistsNotif;
    public bool isGoalTile = false;
    public bool goalTriggered = false;
    private GroundTile[] _neighbors = new GroundTile[6];

    // ── Handling Updates ──────────────────────────────────────────────
    private bool triggerBeatNextUpdate = false;
    public double visualDelay = 0.0;
    public static event Action OnTowerChangeMade;

    // ── Fading variables ──────────────────────────────────────────────
    [SerializeField] private float fadeDuration = 1f;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private float fadeDelay = 0f;
    private Color fadeStartColor;
    private Color fadeTargetColor;

    // ══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════════════

    
    /// <summary>
    /// Called by TileMapConstructor after all tiles are created.
    /// Caches references to neighboring tiles for fast pulse propagation.
    /// </summary>
    public void CacheNeighbors()
    {
        if (tileCoordinate == null) return;

        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate neighborCoord = Coordinates.Instance.GetNeighbor(tileCoordinate, dir, 1);
            if (neighborCoord != null && neighborCoord.go != null)
            {
                _neighbors[dir] = neighborCoord.go.GetComponent<GroundTile>();
            }
            else
            {
                _neighbors[dir] = null;
            }
        }
    }
    
    /// <summary>
    /// Returns the cached neighbor GroundTile in the given direction, or null if none exists.
    /// </summary>
    public GroundTile GetNeighbor(int direction)
    {
        return _neighbors[direction];
    }

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

    void OnEnable()
    {
        TempoHandler.TriggerBeat += BeatRecieved;
    }

    void LateUpdate()
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

        visualDelay -= (double)Time.deltaTime;

        // Handle color fading
        if (isFading)
        {
            if (fadeDelay <= 0)
            {
                fadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(fadeTimer / fadeDuration);

                if (SelectionHandler.currentSelectedTile != this && SelectionHandler.currentHoveredTile != this)
                {
                    Color target;
                    if (isInfoLowlighted)
                        target = infoLowlightMaterialColor;
                    else if (isLowlighted)
                        target = lowlightMaterialColor;
                    else
                        target = fadeTargetColor;

                    tileRenderer.material.color = Color.Lerp(fadeStartColor, target, t);
                }

                if (t >= 1f)
                {
                    isFading = false;
                }
            }
            else
            {
                fadeDelay -= Time.deltaTime;
            }
        }

        // If pulse happened last beat, propagate pulse to next tiles on next frame
        if (pulsesCached.Count != 0 && visualDelay <= 0.0)
        {
            foreach (Pulse pulse in pulsesCached)
            {
                if (pulse.delay > 0 && pulse.life != 0)
                {
                    pulse.delay -= 1;
                    pulse.life--;

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

                        // Queue a visual-only pulse for the next beat cycle.
                        // source=true prevents re-triggering the tower
                        // life=0 prevents propagation to neighboring tiles
                        Pulse visualPulse = new Pulse(pulse.direction, source: true, life: 0);
                        pulses.Add(visualPulse);
                    }
                    else
                    {
                        pulses.Add(pulse);
                    }
                    continue;
                }

                StartFade(beatMaterialColor, originalColor, pulse.direction);
                PropagatePulse(pulse);
            }
            pulsesCached.Clear();
        }

        if (triggerBeatNextUpdate)
        {
            triggerBeatNextUpdate = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Pulse / Beat
    // ══════════════════════════════════════════════════════════════════

    private void StartFade(Color from, Color to, int direction)
    {
        if (SelectionHandler.currentSelectedTile != this && SelectionHandler.currentHoveredTile != this)
        {
            tileRenderer.material.color = activeBeatMaterialColor;
        }

        fadeStartColor = from;
        fadeTargetColor = to;
        fadeTimer = 0f;
        isFading = true;
        fadeDelay = (float)TempoHandler.beatLength;

        if (tower != null)
        {
            tower.AnimatePulse(direction);
        }
    }

    public void SchedulePulse(Pulse pulse)
    {
        foreach (Pulse p in pulses)
        {
            if (p.direction == pulse.direction && p.delay == pulse.delay && !pulse.source && !p.source)
                return;
        }

        pulse.originTile = tileCoordinate;
        pulses.Add(pulse);

        if (tower != null && pulse.delay <= 0)
        {
            pulse.continuous = false;
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

    public void BeatRecieved()
    {
        visualDelay = TempoHandler.beatLength * 1 / 2;

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
        if (!pulse.continuous && !pulse.source) return;
        if (pulse.life == 0) return;

        GroundTile neighbor = _neighbors[pulse.direction];
        if (neighbor == null) return;

        int newLife = pulse.life > 0 ? pulse.life - 1 : pulse.life;
        Pulse nextPulse = new Pulse(pulse.direction, life: newLife);
        neighbor.SchedulePulse(nextPulse);
    }

    // ══════════════════════════════════════════════════════════════════
    // Selection / Highlighting
    // ══════════════════════════════════════════════════════════════════

    public void Highlight()
    {
        tileRenderer.material.color = highlightMaterialColor;
    }

    public void Lowlight()
    {
        isLowlighted = true;
        tileRenderer.material.color = lowlightMaterialColor;
    }

    public void InfoLowlight()
    {
        isInfoLowlighted = true;
        tileRenderer.material.color = infoLowlightMaterialColor;
    }

    public void Select()
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = selectedMaterialColor;
        }
    }

    public void Deselect()
    {
        // Restores to lowlight color if flagged, otherwise originalColor
        if (tileRenderer != null)
        {
            if (isInfoLowlighted)
                tileRenderer.material.color = infoLowlightMaterialColor;
            else if (isLowlighted)
                tileRenderer.material.color = lowlightMaterialColor;
            else
                tileRenderer.material.color = originalColor;
        }
    }

    public void RemoveInfoLowlight()
    {
        isInfoLowlighted = false;
    }

    /// <summary>
    /// Fully clears all highlight/lowlight state and restores to originalColor.
    /// Use this when intentionally removing lowlights, not for hover cleanup.
    /// </summary>
    public void ClearAllHighlights()
    {
        isLowlighted = false;
        isInfoLowlighted = false;
        if (tileRenderer != null)
            tileRenderer.material.color = originalColor;
    }

    // ══════════════════════════════════════════════════════════════════
    // Tower Management
    // ══════════════════════════════════════════════════════════════════

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

    public void AddTowerToTile(TowerType type)
    {
        switch (type)
        {
            case TowerType.Source:
                tower = Instantiate(TileMapConstructor.Instance.sourceTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Mono:
                tower = Instantiate(TileMapConstructor.Instance.monoTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Splitter:
                tower = Instantiate(TileMapConstructor.Instance.splitterTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Sink:
                tower = Instantiate(TileMapConstructor.Instance.sinkTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Lobber:
                tower = Instantiate(TileMapConstructor.Instance.lobberTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Sprayer:
                tower = Instantiate(TileMapConstructor.Instance.sprayerTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Buffer:
                tower = Instantiate(TileMapConstructor.Instance.bufferTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Switcher:
                tower = Instantiate(TileMapConstructor.Instance.switcherTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Passer:
                tower = Instantiate(TileMapConstructor.Instance.passerTowerPrefab, transform).GetComponent<Tower>();
                break;
            case TowerType.Mirror:
                tower = Instantiate(TileMapConstructor.Instance.mirrorTowerPrefab, transform).GetComponent<Tower>();
                break;
        }

        tower.tile = this;
        tower.ownType = type;
        OnTowerChangeMade?.Invoke();
    }

    public void AddTowerToTile()
    {
        tower = Instantiate(TileMapConstructor.Instance.sourceTowerPrefab, transform).GetComponent<Tower>();
        tower.tile = this;
    }

    // ══════════════════════════════════════════════════════════════════
    // Goal Tiles
    // ══════════════════════════════════════════════════════════════════

    public void SetAsGoalTile(Color goalColor, bool isUntimed)
    {
        tileRenderer.material.color = goalColor;
        originalColor = goalColor;
        fadeTargetColor = goalColor;
        goalTriggered = false;

        if (isUntimed)
        {
            isGoalTile = true;
        }
    }

    public void RemoveGoalTile()
    {
        originalColor = defaultColor;
        fadeTargetColor = defaultColor;
        tileRenderer.material.color = defaultColor;
        isGoalTile = false;
        goalTriggered = false;
    }

    // ══════════════════════════════════════════════════════════════════
    // Static Helpers
    // ══════════════════════════════════════════════════════════════════

    public static void NotifyTowerChangeMade()
    {
        OnTowerChangeMade?.Invoke();
    }
}