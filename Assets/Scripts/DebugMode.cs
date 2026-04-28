using System;
using UnityEngine;

public class DebugMode : MonoBehaviour
{
    public static DebugMode instance;
    public bool DebugOn = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
