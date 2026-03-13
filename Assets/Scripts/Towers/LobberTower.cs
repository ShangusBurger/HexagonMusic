using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class LobberTower : Tower
{
    public int minLobDistance = 2;
    public int maxLobDistance = 8;
    public int lobDelay = 2;

    [SerializeField]
    private int _lobDistance = -1;

    public int lobDistance
    {
        get => _lobDistance;
        set
        {
            _lobDistance = value;
            CacheLobTargets();
        }
    }

    public GameObject lobProjectilePrefab;

    // Cached destination tiles for each of the 6 directions
    private GroundTile[] _lobTargets = new GroundTile[6];

    internal override void Start()
    {
        base.Start();
        // Cache on start in case lobDistance was set before tile reference was ready
        CacheLobTargets();
    }

    internal override void Update()
    {
        base.Update();
    }

    /// <summary>
    /// Caches the 6 possible lob destination tiles based on current lobDistance.
    /// Called automatically when lobDistance changes, and can be called manually
    /// after the tower is moved or loaded.
    /// </summary>
    public void CacheLobTargets()
    {
        if (tile == null || tile.tileCoordinate == null || _lobDistance <= 0)
        {
            // Clear cache if we can't compute targets
            for (int i = 0; i < 6; i++)
                _lobTargets[i] = null;
            return;
        }

        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate targetCoord = Coordinates.Instance.GetNeighbor(tile.tileCoordinate, dir, _lobDistance);
            if (targetCoord != null && targetCoord.go != null)
            {
                _lobTargets[dir] = targetCoord.go.GetComponent<GroundTile>();
            }
            else
            {
                _lobTargets[dir] = null;
            }
        }
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);

        if (_lobDistance <= 0) return;

        // Calculate outgoing direction (opposite of incoming)
        int outDir = (incomingPulse.direction + 3) % 6;

        // Use cached target instead of runtime lookup
        GroundTile targetTile = _lobTargets[outDir];

        if (targetTile != null)
        {
            Pulse lobbedPulse = new Pulse(outDir, continuous: true, source: false, delay: lobDelay);
            targetTile.SchedulePulse(lobbedPulse);
            LaunchProjectile(targetTile, lobbedPulse);
        }
    }

    void LaunchProjectile(GroundTile targetTile, Pulse pulse)
    {
        double flightDuration = TempoHandler.beatLength * lobDelay;

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTile.transform.position;

        GameObject projectile = Instantiate(lobProjectilePrefab, startPos, Quaternion.identity);
        LobProjectile lobScript = projectile.GetComponent<LobProjectile>();

        if (lobScript != null)
        {
            lobScript.Initialize(startPos, targetPos, (float)flightDuration, targetTile, 
                (float)(TempoHandler.nextBeatTime - AudioSettings.dspTime) + .1f);
            lobScript.OriginTile = tile;
        }
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
    }

    public override void SetSelfUI()
    {
        towerUI.SetDropdown("Shaker");
        towerUI.OnSampleSelected("Shaker");
    }
}