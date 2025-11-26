using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerUI : MonoBehaviour
{
    public Image muteButtonImage;

    public Sprite mutedSprite;
    public Sprite unmutedSprite;

    private Tower tower;

    public void Start()
    {
        SelectionHandler.HideAllTowerUI += HideSelf;
    }

    public void SetTargetTower(Tower t)
    {
        tower = t;
    }

    public void RemoveFromReference()
    {
        SelectionHandler.HideAllTowerUI -= HideSelf;
    }

    void HideSelf()
    {
        gameObject.SetActive(false);
    }
}
