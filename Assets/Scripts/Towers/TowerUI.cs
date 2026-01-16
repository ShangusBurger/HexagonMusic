using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;


public class TowerUI : MonoBehaviour
{
    public Image muteButtonImage;

    public Sprite mutedSprite;
    public Sprite unmutedSprite;

    [Header("Sample Selection")]
    [SerializeField] private TMPro.TMP_Dropdown sampleDropdown;

    private Tower tower;

    public void Start()
    {
        SelectionHandler.HideAllTowerUI += HideSelf;
        InitializeDropdown();
    }

    void InitializeDropdown()
    {
        if (sampleDropdown == null) return;

        sampleDropdown.ClearOptions();

        // Add all sample type names to the dropdown
        List<string> options = new List<string>();
        foreach (SampleType type in Enum.GetValues(typeof(SampleType)))
        {
            options.Add(SampleLibrary.FormatSampleName(type));
        }
        sampleDropdown.AddOptions(options);

        // Subscribe to dropdown value changed
        sampleDropdown.onValueChanged.AddListener(OnSampleSelected);

        tower.SetSelfUI();
    }

    public void SetDropdown(SampleType currentType)
    {
        if (sampleDropdown == null) return;

        int index = (int)currentType;
        if (index >= 0 && index < sampleDropdown.options.Count)
        {
            sampleDropdown.value = index;
        }
    }

    void OnSampleSelected(int index)
    {
        if (tower == null || SampleLibrary.Instance == null) return;

        SampleType selectedType = (SampleType)index;
        AudioClip newClip = SampleLibrary.Instance.GetSample(selectedType);

        if (newClip != null)
        {
            tower.playbackClip = newClip;
        }
    }
    public void OnSampleSelected(SampleType type)
    {
        if (tower == null || SampleLibrary.Instance == null) return;

        AudioClip newClip = SampleLibrary.Instance.GetSample(type);

        if (newClip != null)
        {
            tower.playbackClip = newClip;
        }
    }

    public void SetTargetTower(Tower t)
    {
        tower = t;
    }

    public void RemoveFromReference()
    {
        SelectionHandler.HideAllTowerUI -= HideSelf;
        if (sampleDropdown != null)
        {
            sampleDropdown.onValueChanged.RemoveListener(OnSampleSelected);
        }
    }

    void HideSelf()
    {
        gameObject.SetActive(false);
    }
}
