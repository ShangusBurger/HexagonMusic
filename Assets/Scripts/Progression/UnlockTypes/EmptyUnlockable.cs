using UnityEngine;

[CreateAssetMenu(fileName = "EmptyUnlock", menuName = "Unlockables/Empty")]
public class EmptyUnlockable : Unlockable
{
    public override void Unlock()
    {

    }

    public override bool IsUnlocked()
    {
        return false;
    }
}