using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CubeCoordinates;

[CreateAssetMenu(fileName = "NewGoal", menuName = "Goals/SingleLocationExclusive")]
public class SingleLocationGoalExclusive : SingleLocationGoal
{
    public override bool IsComplete()
    {
        if (targetTile.pulses.Count > 0)
        {
            foreach (Pulse p in targetTile.pulses)
            {
                if (!p.source && !stashedDirections.Contains(p.direction))
                {
                    stashedDirections.Add(p.direction);
                    DeconstructGoal();
                    return true;
                }
            }
        }
        return false;
    }
}