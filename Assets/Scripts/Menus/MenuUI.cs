using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    public GameObject menuContents;

    [Header("Save/Load (optional)")]
    [SerializeField] private SaveLoadUI saveLoadUI;

    private bool isMenuOpen = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                // If a sub-panel (map save/load) is open, close that first
                // instead of closing the whole menu
                if (saveLoadUI != null && saveLoadUI.TryCloseActiveSubPanel())
                    return;

                menuContents.SetActive(false);
                isMenuOpen = false;
            }
            else
            {
                menuContents.SetActive(true);
                isMenuOpen = true;
            }
        }
    }

    public void ExitGame()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveProgress();

        Application.Quit();
    }

    public void ResumeGame()
    {
        // Close any open sub-panels first
        if (saveLoadUI != null)
            saveLoadUI.TryCloseActiveSubPanel();

        menuContents.SetActive(false);
        isMenuOpen = false;
    }
}