using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/MultiLocationUntimed")]
public class MultiLocationUntimedGoal : Goal
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
            targetTile.SetAsGoalTile(targetColor);
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
            tile.SetAsGoalTile(targetColor); // Reset visual state if needed
        }
    }

    public override bool IsComplete()
    {
        bool isComplete = true;

        foreach (GroundTile tile in targetTiles)
        {
            if (!tile.goalTriggered)
                isComplete = false;
        }
        
        if (isComplete)
            DeconstructGoal();

        return isComplete;
    }
}