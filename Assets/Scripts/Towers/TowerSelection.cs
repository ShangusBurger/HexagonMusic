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
        [HideInInspector] public GameObject lockedPlaceholder;
    }

    [SerializeField] private List<TowerButtonMapping> towerButtons = new List<TowerButtonMapping>();

    public static TowerSelection Instance;
    [SerializeField] private GameObject prefabLockedButton;

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
        foreach (var mapping in towerButtons)
        {
            if (mapping.lockedPlaceholder == null)
                mapping.lockedPlaceholder = Instantiate(prefabLockedButton, mapping.button.transform.position, new Quaternion(0f, 0f, 0f, 0f), transform);
            
            if (UnlockManager.Instance.IsTowerUnlocked(mapping.towerType))
            {
                mapping.button.gameObject.SetActive(true);
                Destroy(mapping.lockedPlaceholder);
                mapping.lockedPlaceholder = null;
            }
            else
            {
                mapping.button.gameObject.SetActive(false);
            }
        }
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