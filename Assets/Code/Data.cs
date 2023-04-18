using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    public static Data instance;
    public float time;
    public bool flag;
    public int fallTime;

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
        flag = true;
        fallTime = 0;
    }
    
    void Start()
    {
        time = 0;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (MenuController.instance.gameObject.activeSelf)
        {
            Time.timeScale = 0;
        }
        else
        {
            time = time + Time.deltaTime;
        }
    }
}
