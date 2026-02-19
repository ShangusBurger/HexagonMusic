using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "LobOverActiveTowerGoal", menuName = "Goals/LobOverActiveTower")]
public class LobOverActiveTowerGoal : Goal
{
    [Header("Goal Settings")]
    [Tooltip("Number of times a tower must be hit underneath an active lob path to complete this goal.")]
    public int requiredHitCount = 1;

    [Tooltip("If true, only counts hits from non-source pulses (i.e. propagated signals, not self-generated).")]
    public bool requireNonSourcePulse = true;

    [Tooltip("Whether the origin (lobber) and destination tiles count as 'underneath' the path.")]
    public bool excludeEndpoints = true;

    private int currentHitCount = 0;

    // Cache to avoid double-counting the same tower in the same beat
    private HashSet<Tower> towersCountedThisBeat = new HashSet<Tower>();
    private double lastCheckedBeatTime = -1;

    public override void SetupGoal()
    {
        currentHitCount = 0;
        towersCountedThisBeat.Clear();
        lastCheckedBeatTime = -1;
    }

    public override void DeconstructGoal()
    {
        towersCountedThisBeat.Clear();
    }

    public override bool IsComplete()
    {
        // Reset per-beat tracking when a new beat starts
        if (TempoHandler.nextBeatTime != lastCheckedBeatTime)
        {
            towersCountedThisBeat.Clear();
            lastCheckedBeatTime = TempoHandler.nextBeatTime;
        }

        // Check all active lob projectiles
        if (LobProjectile.ActiveProjectiles == null || LobProjectile.ActiveProjectiles.Count == 0)
            return currentHitCount >= requiredHitCount;

        foreach (LobProjectile projectile in LobProjectile.ActiveProjectiles)
        {
            if (projectile == null || projectile.OriginTile == null || projectile.TargetTile == null)
                continue;

            Coordinate originCoord = projectile.OriginTile.tileCoordinate;
            Coordinate targetCoord = projectile.TargetTile.tileCoordinate;

            if (originCoord == null || targetCoord == null)
                continue;

            // Get all hex tiles along the lob path
            List<Coordinate> pathTiles = Coordinates.Instance.GetLine(originCoord, targetCoord);

            foreach (Coordinate coord in pathTiles)
            {
                if (coord == null || coord.go == null)
                    continue;

                // Optionally skip the lobber origin and landing destination
                if (excludeEndpoints && (coord == originCoord || coord == targetCoord))
                    continue;

                GroundTile groundTile = coord.go.GetComponent<GroundTile>();
                if (groundTile == null || groundTile.tower == null)
                    continue;

                Tower tower = groundTile.tower;

                // Skip if we already counted this tower this beat
                if (towersCountedThisBeat.Contains(tower))
                    continue;

                // Check if this tower was activated this beat
                if (!tower.towerAlreadyActivatedThisBeat)
                    continue;

                // Optionally check that the tower was hit by a non-source pulse
                if (requireNonSourcePulse && !TowerWasHitByNonSourcePulse(groundTile))
                    continue;

                // This tower was hit underneath an active lob path!
                towersCountedThisBeat.Add(tower);
                currentHitCount++;

                if (currentHitCount >= requiredHitCount)
                {
                    DeconstructGoal();
                    return true;
                }
            }
        }

        return currentHitCount >= requiredHitCount;
    }

    /// <summary>
    /// Checks whether the tile has any active non-source pulses,
    /// confirming the tower was hit by a propagated signal rather than its own output.
    /// </summary>
    private bool TowerWasHitByNonSourcePulse(GroundTile tile)
    {
        // Check current pulses
        foreach (Pulse p in tile.pulses)
        {
            if (!p.source) return true;
        }
        // Also check cached pulses (being processed this frame)
        foreach (Pulse p in tile.pulsesCached)
        {
            if (!p.source) return true;
        }
        return false;
    }

    public override float GetProgressNormalized()
    {
        if (requiredHitCount <= 0) return 1f;
        return Mathf.Clamp01((float)currentHitCount / requiredHitCount);
    }

    public override string GetProgressText()
    {
        return $"{currentHitCount}/{requiredHitCount}";
    }
}