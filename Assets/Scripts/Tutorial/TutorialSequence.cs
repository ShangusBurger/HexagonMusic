using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An ordered list of TutorialPages shown as a multi-step tutorial.
/// Create via Assets > Create > Tutorial > Sequence.
/// </summary>
[CreateAssetMenu(fileName = "NewTutorialSequence", menuName = "Tutorial/Sequence")]
public class TutorialSequence : ScriptableObject
{
    [Header("Sequence")]
    public string sequenceId;
    public List<TutorialPage> pages = new List<TutorialPage>();

    [Header("Behaviour")]
    [Tooltip("If true, this sequence is shown automatically at game start (for intro tutorials).")]
    public bool autoShowOnStart = false;

    [Tooltip("If true, this auto-start sequence only plays once (skip if already seen).")]
    public bool onlyShowOnce = true;
}