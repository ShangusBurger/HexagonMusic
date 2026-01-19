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
        targetTile.SetAsGoalTile(Color.green);
    }

    public override void DeconstructGoal()
    {
        targetTile.RemoveGoalTile();
    }

    public override bool IsComplete()
    {
        if (targetTile.pulses.Count > 0)
        {
            DeconstructGoal();
            return true;
        }
        return false;
    }
}