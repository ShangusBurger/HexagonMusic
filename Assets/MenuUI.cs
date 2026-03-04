using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : MonoBehaviour
{

    public GameObject menuContents;
    private bool isMenuOpen = false;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
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
        Application.Quit();
    }

    public void ResumeGame()
    {
        menuContents.SetActive(false);
    }
}
