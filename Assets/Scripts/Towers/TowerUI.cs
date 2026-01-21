using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor.PackageManager.UI;
using UnityEngine.Audio;


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
    }

    public void InitializeDropdown()
    {
        if (sampleDropdown == null) return;

        sampleDropdown.ClearOptions();

        // Add all sample names to the dropdown
        List<string> options = new List<string>();
        foreach (AudioSampleEntry entry in SampleLibrary.Instance.samples)
        {
            options.Add(entry.name);
        }
        sampleDropdown.AddOptions(options);

        // Subscribe to dropdown value changed
        sampleDropdown.onValueChanged.AddListener(OnSampleSelected);

        gameObject.SetActive(true);
        tower.SetSelfUI();
        gameObject.SetActive(false);
    }

    public void SetDropdown(string currentSample)
    {
        if (sampleDropdown == null) return;

        int index = sampleDropdown.options.FindIndex(option => option.text == currentSample);

        if (index >= 0 && index < sampleDropdown.options.Count)
        {
            sampleDropdown.value = index;
        }
    }

    void OnSampleSelected(int index)
    {
        if (tower == null || SampleLibrary.Instance == null) return;

        AudioSampleEntry selectedSample = SampleLibrary.Instance.samples[index];
        AudioClip newClip = selectedSample.clip;

        if (newClip != null)
        {
            tower.playbackClip = newClip;
            foreach(AudioSource source in tower._audioSources)
            {
                source.outputAudioMixerGroup = selectedSample.mixer;
            }
        }
    }
    public void OnSampleSelected(string sampleName)
    {
        if (tower == null || SampleLibrary.Instance == null) return;

        AudioSampleEntry selectedSample = SampleLibrary.Instance.sampleLookup[sampleName];
        AudioClip newClip = selectedSample.clip;

        Debug.Log(selectedSample.name);

        if (newClip != null)
        {
            tower.playbackClip = newClip;
            foreach(AudioSource source in tower._audioSources)
            {
                source.outputAudioMixerGroup = selectedSample.mixer;
            }
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
