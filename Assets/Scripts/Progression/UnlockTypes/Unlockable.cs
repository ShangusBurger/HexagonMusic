using UnityEngine;

public abstract class Unlockable : ScriptableObject
{
    [Header("Display Info")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    public abstract void Unlock();
    public abstract bool IsUnlocked();
}