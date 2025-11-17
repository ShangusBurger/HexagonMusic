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
        base.Update();
        //schedules next beat once the instrument has sounded
        if (AudioSettings.dspTime > goalTime)
        {
            TriggerPulses();
            goalTime += TempoHandler.barLength;
            PlayScheduledClip();
        }
    }

    void TriggerPulses()
    {
        tile.SchedulePulse(new Pulse(0, source: true));
        tile.SchedulePulse(new Pulse(2, source: true));   
        tile.SchedulePulse(new Pulse(4, source: true));
    }

    /* internal override void UpdateAudioSignalEffects(Coordinate updatedCoordinate)
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
    } */
}
