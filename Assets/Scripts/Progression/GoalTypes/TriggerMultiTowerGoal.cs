using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/TriggerMultiTower")]
public class TriggerMultiTowerGoal : Goal
{
    public int targetNumber;
    public TowerType targetType;

    public override void SetupGoal()
    {

    }

    public override void DeconstructGoal()
    {

    }

    public override bool IsComplete()
    {
        int activeTowers = 0;
        foreach (Tower t in Tower.allTowers)
        {
            if (t.ownType == targetType)
            {
                if (t.tile.pulses.Count > 0)
                {
                    activeTowers++;
                    if (activeTowers >= targetNumber) return true; 
                }
            }
        }
        return false;
    }
}