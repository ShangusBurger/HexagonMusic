using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerSelection : MonoBehaviour
{
    [SerializeField] private Button monoButton;
    [SerializeField] private Button sourceButton;
    [SerializeField] private Button splitterButton;
    [SerializeField] private Button lobberButton;
    

    public static TowerSelection Instance;

    public void Start()
    {
        Instance = this;
    }

    public void SetTargetTile(GroundTile tile)
    {
        monoButton.onClick.RemoveAllListeners();
        monoButton.onClick.AddListener(() => tile.AddTowerToTile(TowerType.Mono));
        monoButton.onClick.AddListener(() => this.gameObject.SetActive(false));

        sourceButton.onClick.RemoveAllListeners();
        sourceButton.onClick.AddListener(() => tile.AddTowerToTile(TowerType.Source));  
        sourceButton.onClick.AddListener(() => this.gameObject.SetActive(false));

        splitterButton.onClick.RemoveAllListeners();
        splitterButton.onClick.AddListener(() => tile.AddTowerToTile(TowerType.Splitter));  
        splitterButton.onClick.AddListener(() => this.gameObject.SetActive(false));

        lobberButton.onClick.RemoveAllListeners();
        lobberButton.onClick.AddListener(() => tile.AddTowerToTile(TowerType.Lobber));  
        lobberButton.onClick.AddListener(() => this.gameObject.SetActive(false));
    }
}
