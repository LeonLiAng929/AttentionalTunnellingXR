using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
public class DimensionTextBehaviour : MonoBehaviour
{
    
    private OVRCameraRig _cameraRig;
    private void Awake()
    {
        _cameraRig = FindAnyObjectByType<OVRCameraRig>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(_cameraRig.transform);
    }
}

