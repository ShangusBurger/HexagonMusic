using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CubeCoordinates;

public class ClearFieldController : MonoBehaviour
{
    public static ClearFieldController Instance;
    public static event Action OnClearField;

    void Awake()
    {
        Instance = this;
    }

    public void ClearAllTowers()
    {
        // Suppress interaction counting — destroying towers calls
        // InteractionMade() and should not inflate player stats
        Tower.SuppressInteractions = true;

        // Clear undo history since the entire field is being wiped
        if (UndoManager.Instance != null)
            UndoManager.Instance.ClearHistory();

        OnClearField?.Invoke();

        SelectionHandler.HideTowerUIs();
        SelectionHandler.DeselectCurrent();
        SelectionHandler.currentMouseState = MouseState.HandTool;

        if (ToolbarUI.Instance != null)
            ToolbarUI.Instance.SelectHandTool();

        Tower.SuppressInteractions = false;
    }
}