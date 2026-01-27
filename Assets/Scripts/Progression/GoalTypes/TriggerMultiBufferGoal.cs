using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/TriggerMultiTower")]
public class TriggerMultiBufferGoal : Goal
{
    public int targetNumber;
    public List<int> bufferSizes;

    public override void SetupGoal()
    {

    }

    public override void DeconstructGoal()
    {

    }

    public override bool IsComplete()
    {
        bufferSizes = new List<int>();
        foreach (Tower t in Tower.allTowers)
        {
            if (t.ownType == TowerType.Buffer)
            {
                BufferTower bt = t as BufferTower;
                if (bt.goalCompleteFlag && !bufferSizes.Contains(bt.threshold))
                {
                    bufferSizes.Add(bt.threshold);
                    bt.goalCompleteFlag = false;
                }
            }
            if (bufferSizes.Count >= targetNumber) return true;
        }
        return false;
    }
}