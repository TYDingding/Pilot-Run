using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    public static Data instance;
    public float time;
    public GameObject intro;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        time = 0;
    }
    
    // Update is called once per frame
    void Update()
    {
        time = time + Time.deltaTime;
        if (intro)
        {
            if (intro.active)
            {
                time = 0;
            }
        }
    }
}
