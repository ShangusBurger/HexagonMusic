using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerUI : MonoBehaviour
{
    [SerializeField] private Button deleteButton;
    public static TowerUI Instance;

    public void Start()
    {
        Instance = this;
    }

    public void SetTargetTile(GroundTile tile)
    {
        deleteButton.gameObject.SetActive(true);
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => tile.RemoveTower());
        deleteButton.onClick.AddListener(() => this.gameObject.SetActive(false));

        if (tile.tower is SourceTower)
        {
            deleteButton.gameObject.SetActive(false);
        }
    }
}
