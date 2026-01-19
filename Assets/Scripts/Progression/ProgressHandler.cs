using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProgressHandler : MonoBehaviour
{
    public static ProgressHandler Instance;

    public int currentLevel = 0;

    [SerializeField] Goal currentGoal;
    [SerializeField] List<Goal> goalsList = new List<Goal>();

    void Update()
    {
        if (currentGoal == null && currentLevel < goalsList.Count)
        {
            currentGoal = goalsList[0];
            currentGoal.SetupGoal();
        }

        if (currentGoal.IsComplete())
        {
            currentLevel++;
            
        }
        // Check current goal for completion
            // If complete, advance level, set next goal as current
            // Unlock what is needed
    }

    public void AdvanceLevel()
    {
        currentLevel++;
    }

    public void ResetProgress()
    {
        currentLevel = 1;
    }
}
