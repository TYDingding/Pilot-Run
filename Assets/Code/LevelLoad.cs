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
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Level 1")
            {
                SceneManager.LoadScene("Level 2");
            } else if (currentScene == "Level 2")
            {
                SceneManager.LoadScene("Level 1");
            }
        }
    }
}
