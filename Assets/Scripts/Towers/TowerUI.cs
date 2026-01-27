using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class TowerUI : MonoBehaviour
{
    public Image muteButtonImage;
    public Sprite mutedSprite;
    public Sprite unmutedSprite;

    [Header("Sample Selection")]
    [SerializeField] private TMPro.TMP_Dropdown sampleDropdown;

    private Tower tower;
    private List<string> dropdownIndexToSampleName = new List<string>();
    private bool isInitialized = false;

    // Awake is called even when GameObject is inactive - ensures subscription happens
    void Awake()
    {
        UnlockManager.OnSampleUnlocked += OnSampleUnlocked;
    }

    void Start()
    {
        SelectionHandler.HideAllTowerUI += HideSelf;
    }

    // Refresh dropdown whenever UI becomes visible to catch any missed updates
    void OnEnable()
    {
        if (isInitialized)
            RefreshDropdownOptions();
    }

    void OnDestroy()
    {
        SelectionHandler.HideAllTowerUI -= HideSelf;
        UnlockManager.OnSampleUnlocked -= OnSampleUnlocked;
        if (sampleDropdown != null)
            sampleDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }

    void OnSampleUnlocked(string sampleName) => RefreshDropdownOptions();

    public void InitializeDropdown()
    {
        if (sampleDropdown == null) return;
        sampleDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        RefreshDropdownOptions();
        isInitialized = true;
        gameObject.SetActive(true);
        if (tower != null) tower.SetSelfUI();
        gameObject.SetActive(false);
    }

    void RefreshDropdownOptions()
    {
        if (sampleDropdown == null || SampleLibrary.Instance == null) return;

        string currentSelection = null;
        if (dropdownIndexToSampleName.Count > 0 && sampleDropdown.value < dropdownIndexToSampleName.Count)
            currentSelection = dropdownIndexToSampleName[sampleDropdown.value];

        sampleDropdown.ClearOptions();
        dropdownIndexToSampleName.Clear();

        List<string> options = new List<string>();
        foreach (AudioSampleEntry entry in SampleLibrary.Instance.samples)
        {
            if (UnlockManager.Instance == null || UnlockManager.Instance.IsSampleUnlocked(entry.name))
            {
                options.Add(entry.name);
                dropdownIndexToSampleName.Add(entry.name);
            }
        }

        sampleDropdown.AddOptions(options);

        if (!string.IsNullOrEmpty(currentSelection))
        {
            int newIndex = dropdownIndexToSampleName.IndexOf(currentSelection);
            if (newIndex >= 0) sampleDropdown.value = newIndex;
        }
    }

    void OnDropdownValueChanged(int index)
    {
        if (tower == null || SampleLibrary.Instance == null) return;
        if (index < 0 || index >= dropdownIndexToSampleName.Count) return;
        OnSampleSelected(dropdownIndexToSampleName[index]);
    }

    public void SetDropdown(string currentSample)
    {
        if (sampleDropdown == null) return;
        int index = dropdownIndexToSampleName.IndexOf(currentSample);
        if (index >= 0 && index < sampleDropdown.options.Count)
            sampleDropdown.value = index;
        else if (dropdownIndexToSampleName.Count > 0)
            sampleDropdown.value = 0;
    }

    public void OnSampleSelected(string sampleName)
    {
        if (tower == null || SampleLibrary.Instance == null) return;

        if (UnlockManager.Instance != null && !UnlockManager.Instance.IsSampleUnlocked(sampleName))
        {
            var unlocked = UnlockManager.Instance.GetUnlockedSamples();
            if (unlocked.Count > 0) sampleName = unlocked[0];
            else return;
        }

        if (!SampleLibrary.Instance.sampleLookup.TryGetValue(sampleName, out AudioSampleEntry entry))
            return;

        if (entry.clip != null)
        {
            tower.playbackClip = entry.clip;
            foreach (AudioSource source in tower._audioSources)
                source.outputAudioMixerGroup = entry.mixer;
        }
    }

    public void SetTargetTower(Tower t) => tower = t;

    public void RemoveFromReference()
    {
        SelectionHandler.HideAllTowerUI -= HideSelf;
        UnlockManager.OnSampleUnlocked -= OnSampleUnlocked;
        if (sampleDropdown != null)
            sampleDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }

    void HideSelf() => gameObject.SetActive(false);
}