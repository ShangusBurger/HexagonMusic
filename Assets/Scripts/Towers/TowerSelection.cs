using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerSelection : MonoBehaviour
{
    [System.Serializable]
    public class TowerButtonMapping
    {
        public TowerType towerType;
        public Button button;
    }

    [SerializeField] private List<TowerButtonMapping> towerButtons = new List<TowerButtonMapping>();

    public static TowerSelection Instance;

    void Start()
    {
        Instance = this;
        UnlockManager.OnTowerUnlocked += OnTowerUnlocked;
        UnlockManager.OnUnlocksChanged += RefreshButtonVisibility;
        RefreshButtonVisibility();
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        UnlockManager.OnTowerUnlocked -= OnTowerUnlocked;
        UnlockManager.OnUnlocksChanged -= RefreshButtonVisibility;
    }

    void OnTowerUnlocked(TowerType tower)
    {
        foreach (var mapping in towerButtons)
            if (mapping.towerType == tower && mapping.button != null)
            { mapping.button.gameObject.SetActive(true); break; }
    }

    void RefreshButtonVisibility()
    {
        if (UnlockManager.Instance == null) return;
        foreach (var mapping in towerButtons)
            if (mapping.button != null)
                mapping.button.gameObject.SetActive(UnlockManager.Instance.IsTowerUnlocked(mapping.towerType));
    }

    public void SetTargetTile(GroundTile tile)
    {
        foreach (var mapping in towerButtons)
        {
            if (mapping.button == null) continue;
            mapping.button.onClick.RemoveAllListeners();
            TowerType type = mapping.towerType;
            mapping.button.onClick.AddListener(() => tile.AddTowerToTile(type));
            mapping.button.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }
}