using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class MoveWithPlatform : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered!");
        other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Leaved!");
        other.transform.SetParent(null);
    }
}
