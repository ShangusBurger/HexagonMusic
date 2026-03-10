using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Persistent bottom-of-screen toolbar for tool/tower selection.
/// Slot 0 = Hand tool (Q), Slots 1-8 = tower types (keys 1-8).
///
/// Visibility rules:
///   - Hand tool slot: always visible
///   - Unlocked tower slots: visible and fully interactive
///   - Next-to-unlock tower slot: visible with lock icon + small 'i' icon in corner
///   - All other locked tower slots: completely hidden
///
/// The info bubble tooltip ONLY appears when hovering over the small 'i' icon,
/// not the entire button.
/// </summary>
public class ToolbarUI : MonoBehaviour
{
    public static ToolbarUI Instance;

    // ── Serialized types ──────────────────────────────────────────────

    [System.Serializable]
    public class ToolSlot
    {
        [Header("Identity")]
        public bool isHandTool;
        public TowerType towerType;

        [Header("References (assign in Inspector)")]
        public Button button;
        public Image iconImage;
        public Image highlightBorder;      // the PARENT container Image (Option 2)
        public GameObject lockedOverlay;   // lock icon overlay
        public GameObject infoIcon;        // small 'i' icon in corner of the slot
        public GameObject infoBubble;      // tooltip that appears above infoIcon on hover
        public TMP_Text infoBubbleText;
        public GameObject hotkeyIcon;

        [Header("Visuals")]
        public Sprite unlockedIcon;
        public Sprite lockedIcon;
        public Color highlightColor = Color.white;  // per-slot active highlight color

        [Header("Input")]
        public KeyCode hotkey = KeyCode.None;
    }

    [Header("Slots (index 0 = hand tool, 1-8 = tower types)")]
    [SerializeField] private List<ToolSlot> slots = new List<ToolSlot>();

    [Header("Highlight")]
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0f);

    // ── Public state ──────────────────────────────────────────────────

    public TowerType? ActiveTowerType { get; private set; } = null;
    public bool IsHandTool => ActiveTowerType == null;
    public static event Action<TowerType?> OnToolChanged;

    // ── Private ───────────────────────────────────────────────────────

    private Dictionary<TowerType, ToolSlot> towerSlotMap = new Dictionary<TowerType, ToolSlot>();
    private ToolSlot handSlot;
    private ToolSlot eraseSlot;
    private TowerType? visibleLockedType = null;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        foreach (var slot in slots)
        {
            if (slot.isHandTool)
                handSlot = slot;
            else
                towerSlotMap[slot.towerType] = slot;
        }
    }

    void Start()
    {
        foreach (var slot in slots)
        {
            var captured = slot;
            slot.button.onClick.AddListener(() => OnSlotClicked(captured));
            AddInfoIconHoverListeners(slot);
        }

        UnlockManager.OnTowerUnlocked += OnTowerUnlocked;
        UnlockManager.OnUnlocksChanged += RefreshAllSlots;
        ProgressHandler.OnAnyProgressChanged += RefreshAllSlots;

        RefreshAllSlots();
        SelectTowerTool(TowerType.Mono);
    }

    void OnDestroy()
    {
        UnlockManager.OnTowerUnlocked -= OnTowerUnlocked;
        UnlockManager.OnUnlocksChanged -= RefreshAllSlots;
        ProgressHandler.OnAnyProgressChanged -= RefreshAllSlots;
    }

    void Update()
    {
        if (InputFocusGuard.IsInputFieldFocused()) return;

        foreach (var slot in slots)
        {
            if (slot.hotkey != KeyCode.None && Input.GetKeyDown(slot.hotkey))
                OnSlotClicked(slot);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Tool selection
    // ══════════════════════════════════════════════════════════════════

    void OnSlotClicked(ToolSlot slot)
    {
        if (slot.isHandTool)
        {
            SelectHandTool();
            return;
        }

        if (UnlockManager.Instance != null && !UnlockManager.Instance.IsTowerUnlocked(slot.towerType))
            return;

        ActiveTowerType = slot.towerType;
        RefreshHighlights();
        OnToolChanged?.Invoke(ActiveTowerType);
    }

    public void SelectHandTool()
    {
        ActiveTowerType = null;
        RefreshHighlights();
        OnToolChanged?.Invoke(null);
    }

    public void SelectTowerTool(TowerType type)
    {
        if (UnlockManager.Instance != null && !UnlockManager.Instance.IsTowerUnlocked(type))
            return;
        ActiveTowerType = type;
        RefreshHighlights();
        OnToolChanged?.Invoke(ActiveTowerType);
    }

    // ══════════════════════════════════════════════════════════════════
    // Visual refresh
    // ══════════════════════════════════════════════════════════════════

    void RefreshHighlights()
    {
        foreach (var slot in slots)
        {
            bool isActive;
            if (slot.isHandTool)
                isActive = IsHandTool;
            else
                isActive = ActiveTowerType.HasValue && ActiveTowerType.Value == slot.towerType;

            if (slot.highlightBorder != null)
                slot.highlightBorder.color = isActive ? slot.highlightColor : inactiveColor;
        }
    }

    void RefreshAllSlots()
    {
        visibleLockedType = FindNextLockedTowerToShow();

        foreach (var slot in slots)
        {
            if (slot.isHandTool)
            {
                slot.button.gameObject.SetActive(true);
                // For Option 2 hierarchy, the highlight is the parent — ensure it's active
                if (slot.highlightBorder != null)
                    slot.highlightBorder.gameObject.SetActive(true);
                slot.button.interactable = true;
                if (slot.lockedOverlay != null) slot.lockedOverlay.SetActive(false);
                if (slot.infoIcon != null) slot.infoIcon.SetActive(false);
                if (slot.infoBubble != null) slot.infoBubble.SetActive(false);
                if (slot.iconImage != null && slot.unlockedIcon != null)
                    slot.iconImage.sprite = slot.unlockedIcon;
                continue;
            }

            bool unlocked = UnlockManager.Instance != null
                && UnlockManager.Instance.IsTowerUnlocked(slot.towerType);

            if (unlocked)
            {
                // Unlocked: visible, normal icon, no lock, no info icon
                SetSlotContainerActive(slot, true);
                slot.button.interactable = true;
                if (slot.lockedOverlay != null) slot.lockedOverlay.SetActive(false);
                if (slot.infoIcon != null) slot.infoIcon.SetActive(false);
                if (slot.infoBubble != null) slot.infoBubble.SetActive(false);
                if (slot.hotkeyIcon != null) slot.hotkeyIcon.SetActive(true);
                if (slot.iconImage != null && slot.unlockedIcon != null)
                    slot.iconImage.sprite = slot.unlockedIcon;
            }
            else if (visibleLockedType.HasValue && visibleLockedType.Value == slot.towerType)
            {
                // Next-to-unlock: visible with lock overlay + info icon
                SetSlotContainerActive(slot, true);
                slot.button.interactable = true;
                if (slot.lockedOverlay != null) slot.lockedOverlay.SetActive(true);
                if (slot.infoIcon != null) slot.infoIcon.SetActive(true);
                if (slot.infoBubble != null) slot.infoBubble.SetActive(false);
                if (slot.hotkeyIcon != null) slot.hotkeyIcon.SetActive(false);
                if (slot.infoBubbleText != null)
                    slot.infoBubbleText.text = GetUnlockHintText(slot.towerType);
                if (slot.iconImage != null && slot.lockedIcon != null)
                    slot.iconImage.sprite = slot.lockedIcon;
            }
            else
            {
                // Other locked: completely hidden
                SetSlotContainerActive(slot, false);
                if (slot.infoBubble != null) slot.infoBubble.SetActive(false);
            }
        }

        RefreshHighlights();
    }

    /// <summary>
    /// In Option 2 hierarchy, the highlight is the parent of the button.
    /// We need to show/hide the parent container, not just the button.
    /// </summary>
    void SetSlotContainerActive(ToolSlot slot, bool active)
    {
        if (slot.highlightBorder != null)
            slot.highlightBorder.gameObject.SetActive(active);
        else
            slot.button.gameObject.SetActive(active);
    }

    void OnTowerUnlocked(TowerType type)
    {
        RefreshAllSlots();
    }

    // ══════════════════════════════════════════════════════════════════
    // Find the single next locked tower to display
    // ══════════════════════════════════════════════════════════════════

    TowerType? FindNextLockedTowerToShow()
    {
        if (ProgressHandler.Instance == null || UnlockManager.Instance == null)
            return null;

        TowerType? bestType = null;
        int bestGoalsAway = int.MaxValue;

        for (int t = 0; t < ProgressHandler.Instance.TrackCount; t++)
        {
            var state = ProgressHandler.Instance.GetTrackStateByIndex(t);
            if (state == null || state.IsComplete) continue;

            for (int g = state.currentLevel; g < state.track.goals.Count; g++)
            {
                Goal goal = state.track.goals[g];
                foreach (var reward in goal.rewards)
                {
                    if (reward is TowerUnlockable tu && !UnlockManager.Instance.IsTowerUnlocked(tu.towerType))
                    {
                        int goalsAway = g - state.currentLevel;
                        if (goalsAway < bestGoalsAway)
                        {
                            bestGoalsAway = goalsAway;
                            bestType = tu.towerType;
                        }
                    }
                }
            }
        }

        return bestType;
    }

    // ══════════════════════════════════════════════════════════════════
    // Tooltip hover — attached to the small 'i' icon ONLY
    // ══════════════════════════════════════════════════════════════════

    void AddInfoIconHoverListeners(ToolSlot slot)
    {
        if (slot.isHandTool || slot.infoIcon == null || slot.infoBubble == null) return;

        // Attach EventTrigger to the info icon, NOT the button
        EventTrigger trigger = slot.infoIcon.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slot.infoIcon.AddComponent<EventTrigger>();

        var capturedSlot = slot;

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) => OnInfoIconHoverEnter(capturedSlot));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) => OnInfoIconHoverExit(capturedSlot));
        trigger.triggers.Add(exitEntry);
    }

    void OnInfoIconHoverEnter(ToolSlot slot)
    {
        if (slot.isHandTool) return;
        if (UnlockManager.Instance != null && UnlockManager.Instance.IsTowerUnlocked(slot.towerType))
            return;
        if (!visibleLockedType.HasValue || visibleLockedType.Value != slot.towerType)
            return;

        if (slot.infoBubble != null)
        {
            slot.infoBubble.SetActive(true);
            if (slot.infoBubbleText != null)
                slot.infoBubbleText.text = GetUnlockHintText(slot.towerType);
        }
    }

    void OnInfoIconHoverExit(ToolSlot slot)
    {
        if (slot.infoBubble != null)
            slot.infoBubble.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // Unlock hint text
    // ══════════════════════════════════════════════════════════════════

    string GetUnlockHintText(TowerType type)
    {
        if (ProgressHandler.Instance == null)
            return "Keep playing to unlock!";

        for (int t = 0; t < ProgressHandler.Instance.TrackCount; t++)
        {
            var state = ProgressHandler.Instance.GetTrackStateByIndex(t);
            if (state == null) continue;

            for (int g = state.currentLevel; g < state.track.goals.Count; g++)
            {
                Goal goal = state.track.goals[g];
                foreach (var reward in goal.rewards)
                {
                    if (reward is TowerUnlockable tu && tu.towerType == type)
                    {
                        if (goal is PlaceOrDeleteTowers pdGoal)
                        {
                            int remaining = pdGoal.requiredInteractionCount;
                            return $"Place or delete {remaining} towers\nto unlock this tower";
                        }
                        return goal.displayText;
                    }
                }
            }
        }

        return "Keep playing to unlock!";
    }
}