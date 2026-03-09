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
        // Invoke event for any listeners
        OnClearField?.Invoke();
        
        // Hide any open UI
        SelectionHandler.HideTowerUIs();
        SelectionHandler.DeselectCurrent();
        SelectionHandler.currentMouseState = MouseState.HandTool;

        // Also reset toolbar to hand tool
        if (ToolbarUI.Instance != null)
            ToolbarUI.Instance.SelectHandTool();
    }
}