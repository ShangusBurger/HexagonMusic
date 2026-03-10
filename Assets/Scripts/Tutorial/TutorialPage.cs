using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// A single tutorial page. Create via Assets > Create > Tutorial > Page.
/// </summary>
[CreateAssetMenu(fileName = "NewTutorialPage", menuName = "Tutorial/Page")]
public class TutorialPage : ScriptableObject
{
    [Header("Content")]
    [TextArea(3, 8)]
    public string tutorialText;

    [Tooltip("Short looping video clip (.mp4/.webm). Convert gifs to video for Unity compatibility.")]
    public VideoClip videoClip;

    [Header("Association (optional)")]
    [Tooltip("If linked to a tower unlock, set this so the tutorial can be offered from the reward panel.")]
    public TowerType associatedTower = (TowerType)(-1);

    [Tooltip("If linked to a sample unlock, set the sample name.")]
    public string associatedSample;

    /// <summary>True if this page is linked to a specific unlock reward.</summary>
    public bool HasAssociation => (int)associatedTower >= 0 || !string.IsNullOrEmpty(associatedSample);
}