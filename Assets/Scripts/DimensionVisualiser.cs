using UnityEngine;
using System.Collections.Generic;
using System;
using Meta.XR.BuildingBlocks;
using TMPro;

public class DimensionVisualiser : MonoBehaviour
{
    public static DimensionVisualiser instance;
    public GameObject dimensionVisualPrefab;
    public SpatialAnchorCoreBuildingBlock _spatialAnchorCore;
    private OVRCameraRig _cameraRig;
    public List<OVRSpatialAnchor> anchorList = new List<OVRSpatialAnchor>();
    public List<GameObject> visualiserObjects = new List<GameObject>();
    private void Awake()
    {
        instance = this;
        _cameraRig = FindAnyObjectByType<OVRCameraRig>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnEnable()
    {
        if (_spatialAnchorCore != null)
        {
            _spatialAnchorCore.OnAnchorCreateCompleted.AddListener(HandleAnchorCreated);
            //_spatialAnchorCore.OnAnchorEraseCompleted.AddListener(HandleAnchorErased);
            // CRITICAL: Subscribe to the load completion event
            _spatialAnchorCore.OnAnchorsLoadCompleted.AddListener(HandleAnchorsLoaded);
            _spatialAnchorCore.OnAnchorsEraseAllCompleted.AddListener(HandleAllAnchorsErased);
        }
    }

    private void HandleAllAnchorsErased(OVRSpatialAnchor.OperationResult arg0)
    {
        foreach (GameObject go in visualiserObjects)
        {
            Destroy(go);
        }
        visualiserObjects.Clear();
        anchorList.Clear();
    }

    private void HandleAnchorsLoaded(List<OVRSpatialAnchor> arg0)
    {
        anchorList.Clear();
        anchorList = arg0;
        foreach (GameObject go in visualiserObjects)
        {
            Destroy(go);
        }
   
        for (int i = 0; i < anchorList.Count; i++)
        {
            for (int j = i + 1; j < anchorList.Count; j++)
            {
                Vector3[] lineToDraw = new Vector3[2];
                lineToDraw[0] = anchorList[i].transform.position;
                lineToDraw[1] = anchorList[j].transform.position;
                GameObject go = Instantiate(dimensionVisualPrefab);
                TMP_Text dimensionText = go.GetComponentInChildren<TMP_Text>();
                dimensionText.text = (anchorList[i].transform.position - anchorList[j].transform.position).magnitude.ToString() + " m";
                LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
                lineRenderer.SetPositions(lineToDraw);
                
                Vector3 midPoint = (anchorList[i].transform.position + anchorList[j].transform.position)/2f + new Vector3(0,.1f,0);
                dimensionText.gameObject.transform.position = midPoint;
                visualiserObjects.Add(go);
            }
        }
        
        
    }

    private void HandleAnchorCreated(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        foreach (GameObject go in visualiserObjects)
        {
            Destroy(go);
        }
        anchorList.Add(anchor);
        for (int i = 0; i < anchorList.Count; i++)
        {
            for (int j = i + 1; j < anchorList.Count; j++)
            {
                Vector3[] lineToDraw = new Vector3[2];
                lineToDraw[0] = anchorList[i].transform.position;
                lineToDraw[1] = anchorList[j].transform.position;
                GameObject go = Instantiate(dimensionVisualPrefab);
                TMP_Text dimensionText = go.GetComponentInChildren<TMP_Text>();
                dimensionText.text = (anchorList[i].transform.position - anchorList[j].transform.position).magnitude.ToString() + " m";
                LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
                lineRenderer.SetPositions(lineToDraw);
                
                Vector3 midPoint = (anchorList[i].transform.position + anchorList[j].transform.position)/2f;
                dimensionText.gameObject.transform.position = midPoint;
                visualiserObjects.Add(go);
            }
        }
    }

    private void OnDisable()
    {
        if (_spatialAnchorCore != null)
        {
            _spatialAnchorCore.OnAnchorCreateCompleted.RemoveListener(HandleAnchorCreated);
            //_spatialAnchorCore.OnAnchorEraseCompleted.RemoveListener(HandleAnchorErased);
            _spatialAnchorCore.OnAnchorsLoadCompleted.RemoveListener(HandleAnchorsLoaded);
            _spatialAnchorCore.OnAnchorsEraseAllCompleted.RemoveListener(HandleAllAnchorsErased);
        }
    }


    public void HideAllVisualisers()
    {
        foreach (var go in visualiserObjects)
        {
            go.SetActive(!go.activeSelf);
        }
    }

}
