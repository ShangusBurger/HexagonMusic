using System.Collections;
using System.Collections.Generic;
using CubeCoordinates;
using UnityEngine;

public class SourceTower : Tower
{
    [SerializeField] private int sourceTowerReach = 12;
    internal override void Start()
    {
        base.Start();
        if (!_audioSources[0].isPlaying || !_audioSources[1].isPlaying)
        {
            PlayScheduledClip();
        }
    }
    
    internal override void Update()
    {
        //schedules next beat once the instrument has sounded
        if (AudioSettings.dspTime > goalTime)
        {
            TriggerPulses();
            TriggerDependentTowers();
            ScheduleBeat();
        }
    }

    //schedules Source beat for next bar downbeat
    internal override void ScheduleBeat()
    {
        goalTime += TempoHandler.barLength;
        base.ScheduleBeat();
    }

    internal override void UpdateAudioSignalEffects(Coordinate updatedCoordinate)
    {
        Coordinate thisCoordinate = tile.tileCoordinate;

        // Collect Lines of Coordinates in 3 directions outward from the Source Tower.
        List<Coordinate> xLine = Coordinates.Instance.GetLine(thisCoordinate, Coordinates.Instance.GetNeighbor(thisCoordinate, 0, sourceTowerReach));
        List<Coordinate> yLine = Coordinates.Instance.GetLine(thisCoordinate, Coordinates.Instance.GetNeighbor(thisCoordinate, 2, sourceTowerReach));
        List<Coordinate> zLine = Coordinates.Instance.GetLine(thisCoordinate, Coordinates.Instance.GetNeighbor(thisCoordinate, 4, sourceTowerReach));

        // If the updated coordinate is in any of these lines, do some updating.
        if (xLine.Contains(updatedCoordinate) || yLine.Contains(updatedCoordinate) || zLine.Contains(updatedCoordinate))
        {
            int cubeDistance = (int)Cubes.GetDistanceBetweenTwoCubes(thisCoordinate.cube, updatedCoordinate.cube);
            Tower affectedTower = updatedCoordinate.go.GetComponent<GroundTile>().tower;
            affectedTowers.Add((affectedTower, cubeDistance));

            double idealGoalTime = this.goalTime - TempoHandler.barLength + ((double)cubeDistance * TempoHandler.beatLength);

            // Set trigger of affected tower after checking how far into the bar it has been placed
            if (idealGoalTime > AudioSettings.dspTime + .20)
            {
                affectedTower.goalTime = this.goalTime - TempoHandler.barLength + ((double)cubeDistance * TempoHandler.beatLength);
                affectedTower.ScheduleBeat();
            }
            else
            {
                affectedTower.goalTime = this.goalTime + ((double)cubeDistance * TempoHandler.beatLength);
            }
        }
    }

    void TriggerPulses()
    {
        tile.SchedulePulse(0);
        tile.SchedulePulse(2);
        tile.SchedulePulse(4);
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
}
