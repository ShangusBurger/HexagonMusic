using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuUI : MonoBehaviour
{

    public GameObject menuContents;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuContents.SetActive(true);
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
