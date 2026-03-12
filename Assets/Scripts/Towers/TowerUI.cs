using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using TMPro;

public class TowerUI : MonoBehaviour
{
    [Header("Mute Button")]
    [SerializeField] private Button muteButton;
    public Image muteButtonImage;
    public Sprite mutedSprite;
    public Sprite unmutedSprite;

    [Header("Sample Selection")]
    [SerializeField] private TMP_Dropdown sampleDropdown;

    [Header("Locked Sound")]
    [SerializeField] private Sprite lockSprite;            // lock icon from your sprites
    [SerializeField] private GameObject lockedSoundTooltip; // tooltip panel near dropdown
    [SerializeField] private TMP_Text lockedSoundTooltipText;

    [Header("Tempo Slider (Source Tower Only)")]
    [SerializeField] private GameObject tempoSliderContainer;

    private Tower tower;
    private List<string> dropdownIndexToSampleName = new List<string>();
    private bool isInitialized = false;
    private bool _suppressDropdownCallback = false;
    private int lockedSoundIndex = -1;
    private string lockedSampleName = null;
    private bool isDropdownOpen = false;

    private const string TEMPO_FEATURE_ID = "TempoSlider";

    public static Action OnSampleInteractionMade;

    // ══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════════════

    void Awake()
    {
        UnlockManager.OnSampleUnlocked += OnSampleUnlocked;
        UnlockManager.OnFeatureUnlocked += OnFeatureUnlocked;
    }

    void Start()
    {
        SelectionHandler.HideAllTowerUI += HideSelf;
        
        if (muteButton != null)
            muteButton.onClick.AddListener(OnMuteButtonClicked);
    }

    void OnEnable()
    {
        if (isInitialized)
        {
            _suppressDropdownCallback = true;
            RefreshDropdownOptions();
            _suppressDropdownCallback = false;
        }
        if (lockedSoundTooltip != null)
            lockedSoundTooltip.SetActive(false);

        UpdateMuteButtonSelection();
    }

    void OnDestroy()
    {
        SelectionHandler.HideAllTowerUI -= HideSelf;
        UnlockManager.OnSampleUnlocked -= OnSampleUnlocked;
        UnlockManager.OnFeatureUnlocked -= OnFeatureUnlocked;
        if (sampleDropdown != null)
            sampleDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }

    void Update()
    {
        // Detect when the dropdown list opens so we can inject hover behavior
        if (sampleDropdown == null) return;

        Transform dropdownList = sampleDropdown.transform.Find("Dropdown List");
        if (dropdownList != null && !isDropdownOpen)
        {
            isDropdownOpen = true;
            StartCoroutine(SetupDropdownItemBehavior(dropdownList));
        }
        else if (dropdownList == null && isDropdownOpen)
        {
            isDropdownOpen = false;
            HideLockedSoundTooltip();
        }
    }

    
    void OnMuteButtonClicked()
    {
        if (tower == null) return;
        
        tower.ToggleMute();
        
        // Keep selected while muted, deselect when unmuted
        if (tower.isMuted)
        {
            EventSystem.current.SetSelectedGameObject(muteButton.gameObject);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    void UpdateMuteButtonSelection()
    {
        if (muteButton == null || tower == null) return;
        
        if (tower.isMuted)
        {
            EventSystem.current.SetSelectedGameObject(muteButton.gameObject);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Dropdown initialization
    // ══════════════════════════════════════════════════════════════════

    void OnSampleUnlocked(string sampleName)
    {
        _suppressDropdownCallback = true;
        RefreshDropdownOptions();
        _suppressDropdownCallback = false;
    }

    void OnFeatureUnlocked(string featureId) { }

    public void InitializeDropdown()
    {
        if (sampleDropdown == null) return;
        sampleDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        _suppressDropdownCallback = true;
        RefreshDropdownOptions();
        isInitialized = true;
        gameObject.SetActive(true);
        if (tower != null) tower.SetSelfUI();
        gameObject.SetActive(false);
        _suppressDropdownCallback = false;
    }

    public void RandomizeSample()
    {
        if (tower == null || UnlockManager.Instance == null || SampleLibrary.Instance == null) 
            return;

        List<string> unlocked = UnlockManager.Instance.GetUnlockedSamples();
        if (unlocked.Count == 0) return;

        string randomSample = unlocked[UnityEngine.Random.Range(0, unlocked.Count)];
        
        _suppressDropdownCallback = true;
        OnSampleSelected(randomSample);
        SetDropdown(randomSample);
        _suppressDropdownCallback = false;
    }

    // ══════════════════════════════════════════════════════════════════
    // Dropdown options — unlocked samples + one locked preview
    // ══════════════════════════════════════════════════════════════════

    void RefreshDropdownOptions()
    {
        if (sampleDropdown == null || SampleLibrary.Instance == null) return;

        string currentSelection = null;
        if (dropdownIndexToSampleName.Count > 0 && sampleDropdown.value < dropdownIndexToSampleName.Count)
            currentSelection = dropdownIndexToSampleName[sampleDropdown.value];

        sampleDropdown.ClearOptions();
        dropdownIndexToSampleName.Clear();
        lockedSoundIndex = -1;
        lockedSampleName = null;

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        // Unlocked samples — normal text entries
        foreach (AudioSampleEntry entry in SampleLibrary.Instance.samples)
        {
            if (UnlockManager.Instance == null || UnlockManager.Instance.IsSampleUnlocked(entry.name))
            {
                options.Add(new TMP_Dropdown.OptionData(entry.name));
                dropdownIndexToSampleName.Add(entry.name);
            }
        }

        // Next locked sample — lock sprite, no text
        string nextLocked = FindNextLockedSample();
        if (nextLocked != null)
        {
            lockedSoundIndex = options.Count;
            lockedSampleName = nextLocked;
            // Use lock sprite as the option image; single space for text so it renders
            options.Add(new TMP_Dropdown.OptionData(" ", lockSprite));
            dropdownIndexToSampleName.Add(nextLocked);
        }

        sampleDropdown.AddOptions(options);

        // Restore previous selection
        if (!string.IsNullOrEmpty(currentSelection))
        {
            int newIndex = -1;
            for (int i = 0; i < dropdownIndexToSampleName.Count; i++)
            {
                if (i != lockedSoundIndex && dropdownIndexToSampleName[i] == currentSelection)
                { newIndex = i; break; }
            }
            if (newIndex >= 0) sampleDropdown.value = newIndex;
        }
    }

    /// <summary>
    /// Called when the dropdown list appears. For EVERY item, removes
    /// TMP_Dropdown's built-in toggle listener (which calls Hide()) and
    /// replaces it so the dropdown stays open after selection.
    /// Also handles locked-item visuals and hover as before.
    /// </summary>
    IEnumerator SetupDropdownItemBehavior(Transform dropdownList)
    {
        yield return null;

        if (dropdownList == null) yield break;

        Transform content = dropdownList.Find("Viewport/Content");
        if (content == null)
            content = dropdownList.Find("Content");
        if (content == null) yield break;

        int activeIndex = 0;
        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf) continue;

            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle == null) continue;

            int capturedIndex = activeIndex;

            // Strip TMP_Dropdown's internal listener that triggers Hide()
            toggle.onValueChanged.RemoveAllListeners();

            if (capturedIndex == lockedSoundIndex)
            {
                // ── Locked item: visual setup + block selection ──
                SetupLockedItemVisual(child.gameObject);
                AttachHoverEvents(child.gameObject);

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (!isOn) return;
                    // Immediately revert the toggle visual — don't touch the value
                    toggle.SetIsOnWithoutNotify(false);
                    RestoreActiveToggle(content);
                });
            }
            else
            {
                // ── Normal item: apply selection, stay open ──
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (!isOn)
                    {
                        // Clicking the already-selected item turns it off. Force it back on.
                        toggle.SetIsOnWithoutNotify(true);
                        return;
                    }

                    if (capturedIndex < 0 || capturedIndex >= dropdownIndexToSampleName.Count)
                        return;

                    // Turn off the previously selected toggle before applying new value
                    ClearAllToggles(content);
                    toggle.SetIsOnWithoutNotify(true);

                    // Set the value — this fires onValueChanged →
                    // OnDropdownValueChanged handles sample swap + stats
                    sampleDropdown.value = capturedIndex;
                });
            }

            activeIndex++;
        }
    }

    /// <summary>
    /// Turns off all toggles in the dropdown content without triggering callbacks.
    /// Called before activating the newly selected toggle.
    /// </summary>
    void ClearAllToggles(Transform content)
    {
        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf) continue;
            Toggle t = child.GetComponent<Toggle>();
            if (t != null)
                t.SetIsOnWithoutNotify(false);
        }
    }

    /// <summary>
    /// Re-enables the toggle that matches the current dropdown value.
    /// Used after reverting a locked-item click.
    /// </summary>
    void RestoreActiveToggle(Transform content)
    {
        int currentVal = sampleDropdown.value;
        int idx = 0;
        foreach (Transform child in content)
        {
            if (!child.gameObject.activeSelf) continue;
            Toggle t = child.GetComponent<Toggle>();
            if (t == null) continue;
            if (idx == currentVal)
            {
                t.SetIsOnWithoutNotify(true);
                return;
            }
            idx++;
        }
    }


    /// <summary>
    /// Replaces the locked item's text with the lock sprite image.
    /// </summary>
    void SetupLockedItemVisual(GameObject itemGO)
    {
        // Clear the text
        TMP_Text itemText = itemGO.GetComponentInChildren<TMP_Text>();
        if (itemText != null)
            itemText.text = "";

        // Try to find an existing Image for the item (some templates have one)
        // If not, create one as a child of the item
        Transform existingImage = itemGO.transform.Find("Lock Icon");
        if (existingImage == null && lockSprite != null)
        {
            GameObject lockGO = new GameObject("Lock Icon");
            lockGO.transform.SetParent(itemGO.transform, false);

            RectTransform rt = lockGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(24f, 24f);
            rt.anchoredPosition = Vector2.zero;

            Image img = lockGO.AddComponent<Image>();
            img.sprite = lockSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
    }

    /// <summary>
    /// Attaches PointerEnter/Exit events to the locked dropdown item
    /// so the tooltip appears on hover.
    /// </summary>
    void AttachHoverEvents(GameObject itemGO)
    {
        EventTrigger trigger = itemGO.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = itemGO.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) => ShowLockedSoundTooltip());
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) => HideLockedSoundTooltip());
        trigger.triggers.Add(exitEntry);
    }

    // ══════════════════════════════════════════════════════════════════
    // Locked sound tooltip
    // ══════════════════════════════════════════════════════════════════

    void ShowLockedSoundTooltip()
    {
        if (lockedSoundTooltip == null) return;
        if (lockedSoundTooltipText != null)
            lockedSoundTooltipText.text = GetLockedSampleHintText();
        lockedSoundTooltip.SetActive(true);
    }

    void HideLockedSoundTooltip()
    {
        if (lockedSoundTooltip != null)
            lockedSoundTooltip.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // Dropdown value changed
    // ══════════════════════════════════════════════════════════════════


    void OnDropdownValueChanged(int index)
    {
        if (_suppressDropdownCallback) return;
        if (tower == null || SampleLibrary.Instance == null) return;
        if (index < 0 || index >= dropdownIndexToSampleName.Count) return;

        // Clicked the locked item — revert selection and reopen
        if (index == lockedSoundIndex)
        {
            _suppressDropdownCallback = true;
            int revertIndex = FindCurrentSampleIndex();
            sampleDropdown.value = revertIndex;
            _suppressDropdownCallback = false;
            StartCoroutine(ReopenDropdown());
            return;
        }

        OnSampleSelected(dropdownIndexToSampleName[index]);
        OnSampleInteractionMade?.Invoke();

        // Keep the dropdown open so the player can audition multiple sounds
        StartCoroutine(ReopenDropdown());
    }

    /// <summary>
    /// Waits one frame for TMP_Dropdown to finish closing, then reopens it.
    /// </summary>
    IEnumerator ReopenDropdown()
    {
        yield return null; // wait for the dropdown to fully close

        if (sampleDropdown != null && gameObject.activeInHierarchy)
        {
            sampleDropdown.Show();
        }
    }

    /// <summary>
    /// Finds the dropdown index matching the tower's current playback clip.
    /// </summary>
    int FindCurrentSampleIndex()
    {
        string currentName = GetSampleNameFromClip(tower?.playbackClip);
        for (int i = 0; i < dropdownIndexToSampleName.Count; i++)
        {
            if (i != lockedSoundIndex && dropdownIndexToSampleName[i] == currentName)
                return i;
        }
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════
    // Unlock lookup helpers
    // ══════════════════════════════════════════════════════════════════

    string FindNextLockedSample()
    {
        if (ProgressHandler.Instance == null || UnlockManager.Instance == null)
            return null;

        for (int t = 0; t < ProgressHandler.Instance.TrackCount; t++)
        {
            var state = ProgressHandler.Instance.GetTrackStateByIndex(t);
            if (state == null || state.IsComplete) continue;

            for (int g = state.currentLevel; g < state.track.goals.Count; g++)
            {
                Goal goal = state.track.goals[g];
                foreach (var reward in goal.rewards)
                {
                    if (reward is SampleUnlockable su
                        && !UnlockManager.Instance.IsSampleUnlocked(su.sampleName))
                    {
                        return su.sampleName;
                    }
                }
            }
        }
        return null;
    }

    string GetLockedSampleHintText()
    {
        if (ProgressHandler.Instance == null) return "Keep playing to unlock!";

        for (int t = 0; t < ProgressHandler.Instance.TrackCount; t++)
        {
            var state = ProgressHandler.Instance.GetTrackStateByIndex(t);
            if (state == null || state.IsComplete) continue;

            for (int g = state.currentLevel; g < state.track.goals.Count; g++)
            {
                Goal goal = state.track.goals[g];
                foreach (var reward in goal.rewards)
                {
                    if (reward is SampleUnlockable su
                        && !UnlockManager.Instance.IsSampleUnlocked(su.sampleName))
                    {
                        if (goal is ChangeSoundSamples cssGoal)
                            return $"Change tower sounds {cssGoal.requiredInteractionCount} times\nto unlock {su.sampleName}";
                        return goal.displayText;
                    }
                }
            }
        }
        return "Keep playing to unlock!";
    }

    string GetSampleNameFromClip(AudioClip clip)
    {
        if (clip == null || SampleLibrary.Instance == null) return null;
        foreach (var entry in SampleLibrary.Instance.samples)
        {
            if (entry.clip == clip) return entry.name;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════

    public void SetDropdown(string currentSample)
    {
        if (sampleDropdown == null) return;

        _suppressDropdownCallback = true;
        int index = -1;
        for (int i = 0; i < dropdownIndexToSampleName.Count; i++)
        {
            if (i != lockedSoundIndex && dropdownIndexToSampleName[i] == currentSample)
            { index = i; break; }
        }
        if (index >= 0 && index < sampleDropdown.options.Count)
            sampleDropdown.value = index;
        else if (dropdownIndexToSampleName.Count > 0)
            sampleDropdown.value = 0;
        _suppressDropdownCallback = false;
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
        UnlockManager.OnFeatureUnlocked -= OnFeatureUnlocked;
        if (sampleDropdown != null)
            sampleDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }

    void HideSelf()
    {
        HideLockedSoundTooltip();
        gameObject.SetActive(false);
    }
}