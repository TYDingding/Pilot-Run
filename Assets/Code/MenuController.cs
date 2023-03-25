using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public static MenuController instance;
    
    public GameObject introNote;
    public GameObject pause;
    
    public bool isPause;

    void Awake()
    {
        instance = this;
        if (SceneManager.GetActiveScene().name == "Level 1")
        {
            ShowMenu();
        }
        else
        {
            HideMenu();
        }
    }

    void SwitchMenu(GameObject someMenu)
    {
        introNote.SetActive(false);
        pause.SetActive(false);
        someMenu.SetActive(true);
    }
    
    public void ShowIntroNote()
    {
        SwitchMenu(introNote);
    }
    
    public void ShowPause()
    {
        SwitchMenu(pause);
    }
    
    public void ShowMenu()
    {
        ShowIntroNote();
        gameObject.SetActive(true);
        Time.timeScale = 0;
        isPause = true;
    }
        
    public void HideMenu()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1;
        isPause = false;
    }
    
    public void ShowPauseMenu()
    {
        ShowPause();
        gameObject.SetActive(true);
        Time.timeScale = 0;
        isPause = true;
    }
}
