using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Goal : ScriptableObject
{
    public string displayText;
    public bool triggerNextUnlockable;

    public virtual void SetupGoal()
    {
        // Implement in derived classes
    }

    public virtual void DeconstructGoal()
    {
        // Implement in derived classes
        
    }

    public virtual bool IsComplete()
    {
        // Implement in derived classes
        return false;
    }
}