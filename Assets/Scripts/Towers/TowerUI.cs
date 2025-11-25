using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerUI : MonoBehaviour
{
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button muteButton;
    [SerializeField] private Image muteButtonImage;
    public static TowerUI Instance;

    [SerializeField] private Sprite mutedSprite;
    [SerializeField] private Sprite unmutedSprite;

    [SerializeField] private GroundTile targetedTile;

    public void Start()
    {
        Instance = this;
    }

    public void SetTargetTile(GroundTile tile)
    {
        targetedTile = tile;
        deleteButton.gameObject.SetActive(true);
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => tile.RemoveTower());
        deleteButton.onClick.AddListener(() => this.gameObject.SetActive(false));

        muteButton.onClick.RemoveAllListeners();
        muteButton.onClick.AddListener(() => tile.tower.ToggleMute());
        muteButton.onClick.AddListener(() => ToggleMuteIcon());

        ToggleMuteIcon();

        if (tile.tower is SourceTower)
        {
            deleteButton.gameObject.SetActive(false);
        }
    }

    public void ToggleMuteIcon()
    {
        if (targetedTile.tower.isMuted)
            {
                muteButtonImage.sprite = mutedSprite;
            }
            else
            {
                muteButtonImage.sprite = unmutedSprite;
            }
    }
}
