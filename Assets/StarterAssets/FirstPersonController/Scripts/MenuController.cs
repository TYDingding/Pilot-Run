using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public static MenuController instance;
    
    public GameObject introNote;
    
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            HideMenu();
        }
    }
    
    void SwitchMenu(GameObject someMenu)
    {
        introNote.SetActive(false);
        someMenu.SetActive(true);
    }
    
    public void ShowIntroNote()
    {
        SwitchMenu(introNote);
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
    
}
