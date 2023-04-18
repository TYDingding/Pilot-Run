using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoad : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<FirstPersonController>())
        {
            if (Data.instance.fallTime == 0)
            {
                Data.instance.time = Data.instance.time - 5;
            }
            Data.instance.fallTime = 0;
            
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Level 1")
            {
                SceneManager.LoadScene("Level 2");
            } else if (currentScene == "Level 2")
            {
                SceneManager.LoadScene("Level 3");
            } else if (currentScene == "Level 3")
            {
                SceneManager.LoadScene("Level 4");
            } else if (currentScene == "Level 4")
            {
                SceneManager.LoadScene("Level 5");
            } else
            {
                MenuController.instance.ShowFinishMenu();
            }
        }
    }
}
