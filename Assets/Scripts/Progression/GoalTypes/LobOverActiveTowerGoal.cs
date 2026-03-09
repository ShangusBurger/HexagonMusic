using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "LobOverActiveTowerGoal", menuName = "Goals/LobOverActiveTower")]
public class LobOverActiveTowerGoal : Goal
{
    [Header("Goal Settings")]
    [Tooltip("Number of active towers underneath a single lob's path required to complete this goal.")]
    public int requiredHitCount = 1;

    [Tooltip("If true, only counts hits from non-source pulses (i.e. propagated signals, not self-generated).")]
    public bool requireNonSourcePulse = true;

    [Tooltip("Whether the origin (lobber) and destination tiles count as 'underneath' the path.")]
    public bool excludeEndpoints = true;

    // Per-projectile tracking: which towers have been active underneath each lob during its flight
    private Dictionary<LobProjectile, HashSet<Tower>> towersPerProjectile = new Dictionary<LobProjectile, HashSet<Tower>>();

    // Avoid re-checking the same projectile multiple times in the same beat
    private double lastCheckedBeatTime = -1;

    public override void SetupGoal()
    {
        towersPerProjectile.Clear();
        lastCheckedBeatTime = -1;
    }

    public override void DeconstructGoal()
    {
        towersPerProjectile.Clear();
    }

    public override bool IsComplete()
    {
        // Only re-evaluate once per beat
        if (TempoHandler.nextBeatTime == lastCheckedBeatTime)
            return CheckAnyProjectileMeetsRequirement();

        lastCheckedBeatTime = TempoHandler.nextBeatTime;

        // Prune entries for projectiles that have landed / been destroyed
        PruneStaleProjectiles();

        if (LobProjectile.ActiveProjectiles == null || LobProjectile.ActiveProjectiles.Count == 0)
            return false;

        foreach (LobProjectile projectile in LobProjectile.ActiveProjectiles)
        {
            if (projectile == null || projectile.OriginTile == null || projectile.TargetTile == null)
                continue;

            Coordinate originCoord = projectile.OriginTile.tileCoordinate;
            Coordinate targetCoord = projectile.TargetTile.tileCoordinate;

            if (originCoord == null || targetCoord == null)
                continue;

            // Ensure we have a set for this projectile
            if (!towersPerProjectile.ContainsKey(projectile))
                towersPerProjectile[projectile] = new HashSet<Tower>();

            HashSet<Tower> counted = towersPerProjectile[projectile];

            // Get all hex tiles along the lob path
            List<Coordinate> pathTiles = Coordinates.Instance.GetLine(originCoord, targetCoord);

            foreach (Coordinate coord in pathTiles)
            {
                if (coord == null || coord.go == null)
                    continue;

                if (excludeEndpoints && (coord == originCoord || coord == targetCoord))
                    continue;

                GroundTile groundTile = coord.go.GetComponent<GroundTile>();
                if (groundTile == null || groundTile.tower == null)
                    continue;

                Tower tower = groundTile.tower;

                // Already counted for this specific projectile
                if (counted.Contains(tower))
                    continue;

                if (!tower.towerAlreadyActivatedThisBeat)
                    continue;

                if (requireNonSourcePulse && !TowerWasHitByNonSourcePulse(groundTile))
                    continue;

                // Tower is active underneath this lob — record it
                counted.Add(tower);

                if (counted.Count >= requiredHitCount)
                {
                    DeconstructGoal();
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Remove dictionary entries for projectiles that are no longer in flight.
    /// </summary>
    private void PruneStaleProjectiles()
    {
        List<LobProjectile> stale = null;

        foreach (var kvp in towersPerProjectile)
        {
            if (kvp.Key == null || !LobProjectile.ActiveProjectiles.Contains(kvp.Key))
            {
                if (stale == null) stale = new List<LobProjectile>();
                stale.Add(kvp.Key);
            }
        }

        if (stale != null)
        {
            foreach (var key in stale)
                towersPerProjectile.Remove(key);
        }
    }

    /// <summary>
    /// Quick check without re-scanning — used when called multiple times in the same beat.
    /// </summary>
    private bool CheckAnyProjectileMeetsRequirement()
    {
        foreach (var kvp in towersPerProjectile)
        {
            if (kvp.Value.Count >= requiredHitCount)
                return true;
        }
        return false;
    }

    private bool TowerWasHitByNonSourcePulse(GroundTile tile)
    {
        foreach (Pulse p in tile.pulses)
        {
            if (!p.source) return true;
        }
        foreach (Pulse p in tile.pulsesCached)
        {
            if (!p.source) return true;
        }
        return false;
    }

    public override float GetProgressNormalized()
    {
        if (requiredHitCount <= 0) return 1f;

        int best = 0;
        foreach (var kvp in towersPerProjectile)
        {
            if (kvp.Value.Count > best)
                best = kvp.Value.Count;
        }
        return Mathf.Clamp01((float)best / requiredHitCount);
    }

    public override string GetProgressText()
    {
        int best = 0;
        foreach (var kvp in towersPerProjectile)
        {
            if (kvp.Value.Count > best)
                best = kvp.Value.Count;
        }
        return $"{best}/{requiredHitCount}";
    }
}