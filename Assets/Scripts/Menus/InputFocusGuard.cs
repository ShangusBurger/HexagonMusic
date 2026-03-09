using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Static utility that checks whether a text input field currently has focus.
/// All scripts that read keyboard hotkeys should gate their input with
/// InputFocusGuard.IsInputFieldFocused() to prevent hotkeys from firing
/// while the player is typing.
/// </summary>
public static class InputFocusGuard
{
    /// <summary>
    /// Returns true if any TMP_InputField or legacy InputField is
    /// currently selected in the EventSystem.
    /// </summary>
    public static bool IsInputFieldFocused()
    {
        if (EventSystem.current == null) return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;

        // TMP input fields
        var tmpInput = selected.GetComponent<TMP_InputField>();
        if (tmpInput != null && tmpInput.isFocused) return true;

        // Legacy input fields
        var legacyInput = selected.GetComponent<UnityEngine.UI.InputField>();
        if (legacyInput != null && legacyInput.isFocused) return true;

        return false;
    }
}