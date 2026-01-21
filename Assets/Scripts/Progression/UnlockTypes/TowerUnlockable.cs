using UnityEngine;

[CreateAssetMenu(fileName = "NewTowerUnlock", menuName = "Unlockables/Tower")]
public class TowerUnlockable : Unlockable
{
    [Header("Tower Settings")]
    public TowerType towerType;

    public override void Unlock()
    {
        if (UnlockManager.Instance != null)
            UnlockManager.Instance.UnlockTower(towerType);
    }

    public override bool IsUnlocked()
    {
        return UnlockManager.Instance != null && UnlockManager.Instance.IsTowerUnlocked(towerType);
    }
}