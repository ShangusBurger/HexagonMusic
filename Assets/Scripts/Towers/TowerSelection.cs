using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerSelection : MonoBehaviour
{
    [SerializeField] private Button button1;

    public void SetTargetTile(GroundTile tile)
    {
        button1.onClick.RemoveAllListeners();
        button1.onClick.AddListener(() => tile.AddTowerToTile(TowerType.Mono));
        button1.onClick.AddListener(() => this.gameObject.SetActive(false));
    }
}
