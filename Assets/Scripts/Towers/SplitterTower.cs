using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SplitterTower : Tower
{
    internal override void Update()
    {
        base.Update();
    }

    internal override void OnPulseReceived(Pulse incomingPulse)
    {

        base.OnPulseReceived(incomingPulse);
        directions.Clear();
        directions.Add((incomingPulse.direction + 1) % 6);
        directions.Add((incomingPulse.direction + 5) % 6);

        // Create a new pulse in the redirect direction
        if (directions.Count > 0)
        {
            Pulse redirectedPulse;
            foreach (int direction in directions)
            {
                redirectedPulse = new Pulse(direction, source: true);
                tile.SchedulePulse(redirectedPulse);
            }
        }
    }

    internal override void PlayScheduledClip()
    {
        goalTime = TempoHandler.nextBeatTime;
        base.PlayScheduledClip();
    }
}

    //Adds 3 'shock waves' of signal by enqueuing tiles to be beat in the TempoHandler
   /*  internal override void UpdateSignalEffects()
    {
        if (TempoHandler.tilesToBeat.Count < 1)
        {
            TempoHandler.tilesToBeat.Add(new List<GroundTile>());
        }
            
        Coordinate thisCoordinate = TileMapConstructor.allTiles.GetCoordinateFromWorldPosition(transform.position);
        TempoHandler.tilesToBeat[0].Add(gameObject.GetComponent<GroundTile>());

        for (int i = 1; i <= 6; i++)
        {
            if (TempoHandler.tilesToBeat.Count < i + 1)
            {
                TempoHandler.tilesToBeat.Add(new List<GroundTile>());
            }

            GroundTile xTile = Coordinates.Instance.GetNeighbor(thisCoordinate, 0, i).go.GetComponent<GroundTile>();
            GroundTile yTile = Coordinates.Instance.GetNeighbor(thisCoordinate, 2, i).go.GetComponent<GroundTile>();
            GroundTile zTile = Coordinates.Instance.GetNeighbor(thisCoordinate, 4, i).go.GetComponent<GroundTile>();

            TempoHandler.tilesToBeat[i].Add(xTile);
            TempoHandler.tilesToBeat[i].Add(yTile);
            TempoHandler.tilesToBeat[i].Add(zTile);

            affectedTiles.Add(xTile);
            affectedTiles.Add(yTile);
            affectedTiles.Add(zTile);
        }
    } */

