using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Defines how media and text are arranged on a tutorial page.
/// </summary>
public enum TutorialLayout
{
    MediaLeft_TextRight,
    MediaRight_TextLeft,
    MediaTop_TextBottom,
    MediaBottom_TextTop,
    MediaOnly,
    TextOnly
}

/// <summary>
/// A single media item that can be either an image or video.
/// </summary>
[System.Serializable]
public class TutorialMediaItem
{
    public enum MediaType { Image, Video }

    public MediaType type = MediaType.Image;

    [Tooltip("Used when type is Image")]
    public Sprite image;

    [Tooltip("Used when type is Video (.mp4/.webm)")]
    public VideoClip video;

    [Tooltip("Optional caption displayed below this media item")]
    public string caption;

    /// <summary>Returns true if this item has valid content assigned.</summary>
    public bool HasContent => (type == MediaType.Image && image != null) ||
                              (type == MediaType.Video && video != null);
}

/// <summary>
/// A single tutorial page. Create via Assets > Create > Tutorial > Page.
/// Supports multiple layouts and mixed image/video content.
/// </summary>
[CreateAssetMenu(fileName = "NewTutorialPage", menuName = "Tutorial/Page")]
public class TutorialPage : ScriptableObject
{
    [Header("Layout")]
    [Tooltip("How to arrange media and text on this page.")]
    public TutorialLayout layout = TutorialLayout.MediaLeft_TextRight;

    [Header("Content")]
    [TextArea(3, 8)]
    public string tutorialText;

    [Tooltip("Primary media item (image or video).")]
    public TutorialMediaItem media1;

    [Tooltip("Optional second media item. When set, both display side-by-side or stacked.")]
    public TutorialMediaItem media2;

    [Header("Association (optional)")]
    [Tooltip("If linked to a tower unlock, set this so the tutorial can be offered from the reward panel.")]
    public TowerType associatedTower = (TowerType)(-1);

    [Tooltip("If linked to a sample unlock, set the sample name.")]
    public string associatedSample;

    /// <summary>True if this page is linked to a specific unlock reward.</summary>
    public bool HasAssociation => (int)associatedTower >= 0 || !string.IsNullOrEmpty(associatedSample);

    /// <summary>True if this page has any media content.</summary>
    public bool HasMedia => (media1 != null && media1.HasContent) || (media2 != null && media2.HasContent);

    /// <summary>True if this page has two media items.</summary>
    public bool HasDualMedia => (media1 != null && media1.HasContent) && (media2 != null && media2.HasContent);

    /// <summary>True if this page has text content.</summary>
    public bool HasText => !string.IsNullOrEmpty(tutorialText);
}