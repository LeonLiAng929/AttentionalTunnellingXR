using TMPro;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    public TMP_Text debugText;

    public Transform headCam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        debugText.text = headCam.position.ToString();
    }
}
