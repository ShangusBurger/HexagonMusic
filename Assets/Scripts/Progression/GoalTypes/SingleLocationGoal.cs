using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/SingleLocation")]
public class SingleLocationGoal : Goal
{
    public Vector2 targetHexCoords;
    GroundTile targetTile;

    public override void SetupGoal()
    {
        targetTile = Coordinates.Instance.GetContainer().GetCoordinate(Cubes.ConvertAxialToCube(targetHexCoords)).go.GetComponent<GroundTile>();
        targetTile.SetAsGoalTile(new Color32(159, 250, 157, 255));
    }

    public override void DeconstructGoal()
    {
        targetTile.RemoveGoalTile();
    }

    public override bool IsComplete()
    {
        if (targetTile.pulses.Count > 0)
        {
            foreach (Pulse p in targetTile.pulses)
            {
                if (!p.source)
                {
                    DeconstructGoal();
                    return true;
                }
            }
        }
        return false;
    }
}