using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/MultiTriggerTower")]
public class MultiTriggerTowerGoal : Goal
{
    public TowerType targetTowerType;
    public int targetNumber;

    public override void SetupGoal()
    {

    }

    public override void DeconstructGoal()
    {

    }

    public override bool IsComplete()
    {
        int numPulses;
        foreach (Tower t in Tower.allTowers)
        {
            numPulses = 0;
            if (t.ownType == targetTowerType)
            {
                foreach (Pulse p in t.tile.pulses)
                {
                    if (!p.source)
                    {
                        numPulses++;
                    }
                }
            }

            if (numPulses >= targetNumber) return true;
        }
        return false;
    }
}