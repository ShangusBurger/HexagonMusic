using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEndingGoal", menuName = "Goals/EndingGoal")]
public class EndingGoal : Goal
{

    public override bool showProgressUI => false;

    public override void SetupGoal() { }

    public override bool IsComplete()
    {
        return false; 
    }

    public override void DeconstructGoal() { }
}