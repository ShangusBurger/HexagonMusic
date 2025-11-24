using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CubeCoordinates;
using UnityEngine.UI;

public static class SelectionUtility
{
    public static void DeselectListOfTiles(List<GroundTile> tilesToDeselect)
    {
        foreach (GroundTile tile in tilesToDeselect)
        {
            tile.Deselect();
        }
    }
}
