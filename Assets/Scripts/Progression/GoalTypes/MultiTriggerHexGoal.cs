using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/MultiTriggerHex")]
public class MultiTriggerHexGoal : Goal
{
    [Header("Target Hex")]
    public Vector2 targetHexCoords;
    public int targetNumber;

    [Header("Coloration")]
    public Color32 targetColor;

    private GroundTile targetTile;

    public override void SetupGoal()
    {
        targetTile = Coordinates.Instance.GetContainer()
            .GetCoordinate(Cubes.ConvertAxialToCube(targetHexCoords)).go.GetComponent<GroundTile>();
        targetTile.SetAsGoalTile(targetColor, false);
    }

    public override void DeconstructGoal()
    {
        if (targetTile != null)
            targetTile.RemoveGoalTile();
    }

    public override bool IsComplete()
    {
        if (targetTile == null) return false;

        int numPulses = 0;
        foreach (Pulse p in targetTile.pulses)
        {
            if (!p.source)
                numPulses++;
        }

        if (numPulses >= targetNumber)
        {
            DeconstructGoal();
            return true;
        }

        return false;
    }
}