using System;
using UnityEngine;

public class SingletonObject: MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        Screen.fullScreen = false;
    }
}
