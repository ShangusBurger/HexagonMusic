using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/MultiLocationTimed")]
public class MultiLocationTimedGoal : Goal
{
    public List<Vector2> targetHexCoords;
    List<GroundTile> targetTiles;

    // Coloration
    public Color32 targetColor;
    public Color32 completedColor;

    public override void SetupGoal()
    {
        targetTiles = new List<GroundTile>();
        foreach (Vector2 coord in targetHexCoords)
        {
            GroundTile targetTile = Coordinates.Instance.GetContainer()
                .GetCoordinate(Cubes.ConvertAxialToCube(coord)).go.GetComponent<GroundTile>();
            targetTiles.Add(targetTile);
            targetTile.SetAsGoalTile(targetColor, false);
            targetTile.goalTriggered = false;
        }
        
        // Subscribe to tower changes for reset behavior
        GroundTile.OnTowerChangeMade += ResetTriggeredTiles;
    }

    public override void DeconstructGoal()
    {
        // Unsubscribe from tower changes
        GroundTile.OnTowerChangeMade -= ResetTriggeredTiles;
        
        foreach (GroundTile tile in targetTiles)
        {
            tile.RemoveGoalTile();
        }
    }

    private void ResetTriggeredTiles()
    {
        foreach (GroundTile tile in targetTiles)
        {
            tile.goalTriggered = false;
            tile.SetAsGoalTile(targetColor, false); // Reset visual state if needed
        }
    }

    public override bool IsComplete()
    {
        int i = 0;
        foreach (GroundTile tile in targetTiles)
        {
            if (tile.pulses.Count > 0)
            {
                i++;
            }
        }
        
        if (i >= targetTiles.Count)
        {
            DeconstructGoal();
            return true;
        }
        return false;
    }
}