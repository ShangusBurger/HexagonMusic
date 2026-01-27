using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/SingleLocation")]
public class SingleLocationGoal : Goal
{
    public Vector2 targetHexCoords;
    GroundTile targetTile;

    //Coloration
    public Color32 targetColor;

    public override void SetupGoal()
    {
        targetTile = Coordinates.Instance.GetContainer().GetCoordinate(Cubes.ConvertAxialToCube(targetHexCoords)).go.GetComponent<GroundTile>();
        targetTile.SetAsGoalTile(targetColor);
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