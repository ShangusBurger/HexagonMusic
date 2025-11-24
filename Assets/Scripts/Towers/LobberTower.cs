using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class LobberTower : Tower
{
    public int minLobDistance = 2;
    public int maxLobDistance = 6;
    public int lobDelay = 2;
    
    public GroundTile targetTile;

    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {
        base.OnPulseReceived(incomingPulse);

        // Lob the pulse to the target tile if one is set
        if (targetTile != null && directions.Count > 0)
        {
            // Create a pulse that will continue in the lob direction
            Pulse lobbedPulse = new Pulse(directions[0], continuous: true, source: false, delay: lobDelay);
            targetTile.SchedulePulse(lobbedPulse);
        }
    }

    // Helper method to check if a tile is within valid lob range AND in a hex direction
    public bool IsValidLobTarget(Coordinate target)
    {
        if (target == null || tile == null || tile.tileCoordinate == null)
            return false;

        float distance = Cubes.GetDistanceBetweenTwoCubes(tile.tileCoordinate.cube, target.cube);
        
        // Check if distance is valid
        if (distance < minLobDistance || distance > maxLobDistance)
            return false;

        // Check if target is in one of the 6 hex directions
        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate checkCoord = Coordinates.Instance.GetNeighbor(tile.tileCoordinate, dir, (int)distance);
            if (checkCoord != null && checkCoord.cube == target.cube)
            {
                return true;
            }
        }

        return false;
    }

    // Get all valid tiles within lob range in hex directions
    public List<Coordinate> GetValidLobTargets()
    {
        List<Coordinate> validTargets = new List<Coordinate>();
        
        if (tile == null || tile.tileCoordinate == null)
            return validTargets;

        // Check each of the 6 hex directions
        for (int dir = 0; dir < 6; dir++)
        {
            // Check each distance from min to max
            for (int dist = minLobDistance; dist <= maxLobDistance; dist++)
            {
                Coordinate coord = Coordinates.Instance.GetNeighbor(tile.tileCoordinate, dir, dist);
                if (coord != null)
                {
                    validTargets.Add(coord);
                }
            }
        }

        return validTargets;
    }

    public void SetTargetTile(Coordinate target)
    {
        if (IsValidLobTarget(target))
        {
            targetTile = target.go.GetComponent<GroundTile>();
            // Determine which direction the pulse should continue in
            SetDirection(GetLobDirection(target));
        }
    }

    // Determine which of the 6 hex directions the target is in
    private int GetLobDirection(Coordinate target)
    {
        if (target == null || tile == null || tile.tileCoordinate == null)
            return -1;

        float distance = Cubes.GetDistanceBetweenTwoCubes(tile.tileCoordinate.cube, target.cube);

        for (int dir = 0; dir < 6; dir++)
        {
            Coordinate checkCoord = Coordinates.Instance.GetNeighbor(tile.tileCoordinate, dir, (int)distance);
            if (checkCoord != null && checkCoord.cube == target.cube)
            {
                return dir;
            }
        }

        return -1;
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();

        //pdInstance.SendBang("bang");
    }
}
