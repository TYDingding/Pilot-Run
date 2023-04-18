using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<FirstPersonController>())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Data.instance.flag = false;
            Data.instance.fallTime++;
        }
    }
}
