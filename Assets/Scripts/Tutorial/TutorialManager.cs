using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages tutorial sequences and tracks which have been viewed.
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("All Tutorial Sequences")]
    [SerializeField] private List<TutorialSequence> allSequences = new List<TutorialSequence>();

    [Header("Tower-Specific Tutorials (shown from unlock notifications)")]
    [Tooltip("Map a TowerType to a single TutorialPage for the 'Learn More' button.")]
    [SerializeField] private List<TowerTutorialMapping> towerTutorials = new List<TowerTutorialMapping>();

    // ── Tracking which tutorials have been seen ───────────────────────
    private HashSet<string> seenSequences = new HashSet<string>();
    private static string SeenFilePath => Path.Combine(Application.persistentDataPath, "hexmusic_tutorials_seen.json");

    // ── Events ────────────────────────────────────────────────────────
    public static event Action<TutorialPage> OnShowPage;
    public static event Action OnTutorialClosed;

    // ── Active sequence state ─────────────────────────────────────────
    private TutorialSequence activeSequence;
    private int activePageIndex;

    [Serializable]
    public class TowerTutorialMapping
    {
        public TowerType towerType;
        public TutorialPage page;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSeenData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Defer auto-show by one frame so all UI scripts have
        // finished their Start() and are ready to receive events
        StartCoroutine(AutoShowIntroDelayed());
    }

    private System.Collections.IEnumerator AutoShowIntroDelayed()
    {
        yield return null; // wait one frame

        foreach (var seq in allSequences)
        {
            if (seq.autoShowOnStart)
            {
                if (seq.onlyShowOnce && seenSequences.Contains(seq.sequenceId))
                    continue;

                StartSequence(seq);
                yield break; // Only start one auto-sequence
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Starts a multi-page tutorial sequence.
    /// </summary>
    public void StartSequence(TutorialSequence sequence)
    {
        if (sequence == null || sequence.pages.Count == 0) return;

        activeSequence = sequence;
        activePageIndex = 0;
        ShowCurrentPage();
    }

    /// <summary>
    /// Starts a sequence by its sequenceId.
    /// </summary>
    public void StartSequence(string sequenceId)
    {
        var seq = allSequences.Find(s => s.sequenceId == sequenceId);
        if (seq != null) StartSequence(seq);
    }

    /// <summary>
    /// Shows a single standalone tutorial page (not part of a sequence).
    /// Used for tower-specific tutorials from unlock notifications.
    /// </summary>
    public void ShowSinglePage(TutorialPage page)
    {
        if (page == null) return;

        activeSequence = null;
        activePageIndex = 0;
        OnShowPage?.Invoke(page);
    }

    /// <summary>
    /// Shows the tutorial page associated with a specific tower type,
    /// if one exists. Returns true if a tutorial was found.
    /// </summary>
    public bool ShowTowerTutorial(TowerType type)
    {
        foreach (var mapping in towerTutorials)
        {
            if (mapping.towerType == type && mapping.page != null)
            {
                ShowSinglePage(mapping.page);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if a tutorial page exists for the given tower type.
    /// Used by the notification UI to decide whether to show a "Learn More" button.
    /// </summary>
    public bool HasTowerTutorial(TowerType type)
    {
        foreach (var mapping in towerTutorials)
        {
            if (mapping.towerType == type && mapping.page != null)
                return true;
        }
        return false;
    }

    /// <summary>True if the active sequence has more pages after the current one.</summary>
    public bool HasMorePages()
    {
        if (activeSequence == null) return false;
        return activePageIndex < activeSequence.pages.Count - 1;
    }

    /// <summary>1-based current page number within the active sequence (0 if no sequence).</summary>
    public int CurrentPageNumber => activeSequence != null ? activePageIndex + 1 : 0;

    /// <summary>Total pages in the active sequence (0 if showing a single page).</summary>
    public int TotalPageCount => activeSequence != null ? activeSequence.pages.Count : 0;

    /// <summary>
    /// Called by TutorialUI when the Continue button is pressed.
    /// </summary>
    public void OnContinuePressed()
    {
        if (activeSequence != null)
        {
            activePageIndex++;
            if (activePageIndex < activeSequence.pages.Count)
            {
                ShowCurrentPage();
            }
            else
            {
                // Sequence complete
                MarkSequenceSeen(activeSequence.sequenceId);
                activeSequence = null;
                OnTutorialClosed?.Invoke();
            }
        }
        else
        {
            // Was a single page — just close
            OnTutorialClosed?.Invoke();
        }
    }

    /// <summary>
    /// Resets all "seen" tutorial tracking. Tutorials will auto-show again.
    /// </summary>
    public void ResetSeenTutorials()
    {
        seenSequences.Clear();
        SaveSeenData();
    }

    // ══════════════════════════════════════════════════════════════════
    //  INTERNAL
    // ══════════════════════════════════════════════════════════════════

    private void ShowCurrentPage()
    {
        if (activeSequence == null || activePageIndex >= activeSequence.pages.Count)
            return;

        OnShowPage?.Invoke(activeSequence.pages[activePageIndex]);
    }

    private void MarkSequenceSeen(string sequenceId)
    {
        if (string.IsNullOrEmpty(sequenceId)) return;
        seenSequences.Add(sequenceId);
        SaveSeenData();
    }

    // ══════════════════════════════════════════════════════════════════
    //  PERSISTENCE (simple JSON list of seen sequence IDs)
    // ══════════════════════════════════════════════════════════════════

    [Serializable]
    private class SeenData
    {
        public List<string> ids = new List<string>();
    }

    private void SaveSeenData()
    {
        var data = new SeenData();
        data.ids.AddRange(seenSequences);
        string json = JsonUtility.ToJson(data);
        try { File.WriteAllText(SeenFilePath, json); }
        catch (Exception e) { Debug.LogWarning($"[TutorialManager] Failed to save seen data: {e.Message}"); }
    }

    private void LoadSeenData()
    {
        if (!File.Exists(SeenFilePath)) return;
        try
        {
            string json = File.ReadAllText(SeenFilePath);
            var data = JsonUtility.FromJson<SeenData>(json);
            if (data?.ids != null)
                foreach (string id in data.ids)
                    seenSequences.Add(id);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TutorialManager] Failed to load seen data: {e.Message}");
        }
    }
}