using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProgressionTrack", menuName = "Progression/Track")]
public class ProgressionTrack : ScriptableObject
{
    [Header("Track Identity")]
    public string trackId;
    public string displayName;
    public Sprite trackIcon;
    public Color trackColor = Color.white;

    [Header("Goals (in order)")]
    public List<Goal> goals = new List<Goal>();

    [Header("Slider Display Mode")]
    [Tooltip("If true, the slider shows progress toward completing the current goal. If false, it shows progress toward the next unlock.")]
    public bool showGoalProgress = false;

    [Header("Completion")]
    [TextArea] public string completionMessage = "Track Complete!";
}