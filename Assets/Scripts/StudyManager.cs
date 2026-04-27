using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StudyManager : MonoBehaviour
{
    public List<Transform> debugAnchorList = new List<Transform>();
    public static StudyManager instance;
    public GameObject pathPrefab;
    public Material highlightMaterial;
    public Material defaultMaterial;
        
    public Transform centerCam;
    /*public enum Mode
    {
        SetupMode,
        StudyMode
    }
    
    public Mode currMode = Mode.SetupMode;*/
    public List<Path> paths = new List<Path>();
    public List<Vector3> midPoints = new List<Vector3>();

    private int currentNodeIndex = -1;
    private int nextNodeIndex = -1;
    private int upcomingNodeIndex = -1;
    
    public Path currentPath;
    public Path nextPath;
    
    public float proximityThreshold = 0.5f;

    private bool routeActive = false;
    
    [Serializable]
    public class Path
    {
        public Path(int from, int to, GameObject path)
        {
            this.from  = from;
            this.to = to;
            pathObject = path;
        }
        public int from;
        public int to;
        public GameObject pathObject;
        
    }
    
    
    public void HighlightPath(Path p)
    {
        foreach (MeshRenderer meshRenderer in p.pathObject.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.material = highlightMaterial;
        }
    }

    public void UnhighlightPath(Path p)
    {
        foreach (MeshRenderer meshRenderer in p.pathObject.GetComponentsInChildren<MeshRenderer>())
        {
            meshRenderer.material = defaultMaterial;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        PathMapping();
    }

    // Update is called once per frame
    void Update()
    {
        if (routeActive && nextNodeIndex >= 0 && nextNodeIndex < midPoints.Count)
        {
            // OVR Camera Rig is tracked by Main Camera
            if (centerCam != null) 
            {
                Vector3 targetPosition = midPoints[nextNodeIndex];
                Vector3 userPosition = centerCam.position;
                
                // Keep the check strictly horizontal to ignore player height vs floor anchor differences
                targetPosition.y = 0;
                userPosition.y = 0;
                
                float distance = Vector3.Distance(userPosition, targetPosition);
                if (distance <= proximityThreshold)
                {
                    AdvanceRoute();
                }
            }
        }
    }

    /*public void ChangeMode()
    {
        currMode = currMode == Mode.SetupMode ? Mode.StudyMode : Mode.SetupMode;
        HelperScript.Instance.ShowNotification("Current Mode: " + currMode);
        
    }*/

    public void PathMapping()
    {
        //List<OVRSpatialAnchor> anchorList = DimensionVisualiser.instance.anchorList;
        List<Transform> anchorList = debugAnchorList;
        
        
        ClearPaths();
        midPoints.Clear();
        routeActive = false;

        for (int i = 0; i < anchorList.Count-1; i++)
        {
            
                Vector3 pointA = anchorList[i].transform.position;
                Vector3 pointB = anchorList[i+1].transform.position;


                pointA.y = 0;
                pointB.y = 0;
                Vector3 midPoint = (pointA + pointB) / 2f;
                midPoints.Add(midPoint);
            
        }
        midPoints.Add((anchorList[anchorList.Count-1].transform.position + anchorList[0].transform.position) / 2f);

        for (int i = 0; i < midPoints.Count; i++)
        {
            for (int j = i + 1; j < midPoints.Count; j++)
            {
                Vector3 pointA = midPoints[i];
                Vector3 pointB = midPoints[j];


                pointA.y = 0;
                pointB.y = 0;
                Vector3 midPoint = (pointA + pointB) / 2f;
                Vector3 heading = pointB - pointA; 
                float distance = heading.magnitude; 
                Vector3 direction = heading.normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                Debug.Log(midPoints.Count);
                Debug.Log(i + " " + j);
                GameObject temp = Instantiate(pathPrefab, midPoint, rotation, anchorList[j].transform);
                foreach (Transform t in temp.GetComponentsInChildren<Transform>())
                {
                    t.localScale = new Vector3(t.localScale.x, t.localScale.y, distance);
                }
                Path p = new Path(i, j, temp);
                
                paths.Add(p);
            }
        }

        StartRoute();
    }

    public void ClearPaths()
    {
        foreach (Path p in paths)
        {
            Destroy(p.pathObject);
        }
        paths.Clear();
    }

    public void StartRoute()
    {
        if (paths.Count == 0 || midPoints.Count == 0) return;
        
        // Always start with the first path in the list
        currentNodeIndex = paths[0].from;
        nextNodeIndex = paths[0].to;
        upcomingNodeIndex = GetRandomConnectedNode(nextNodeIndex, currentNodeIndex);
        
        routeActive = true;
        UpdateRouteVisuals();
    }

    public void AdvanceRoute()
    {
        currentNodeIndex = nextNodeIndex;
        nextNodeIndex = upcomingNodeIndex;
        upcomingNodeIndex = GetRandomConnectedNode(nextNodeIndex, currentNodeIndex);
        
        UpdateRouteVisuals();
    }

    private int GetRandomConnectedNode(int node, int excludeNode)
    {
        int totalNodes = midPoints.Count;
        if (totalNodes <= 1) return node;

        List<int> validNodes = new List<int>();
        for (int i = 0; i < totalNodes; i++)
        {
            if (i != node && i != excludeNode)
            {
                validNodes.Add(i);
            }
        }
        
        if (validNodes.Count == 0) 
            return excludeNode;

        return validNodes[UnityEngine.Random.Range(0, validNodes.Count)];
    }

    private Path GetPathBetween(int nodeA, int nodeB)
    {
        int min = Mathf.Min(nodeA, nodeB);
        int max = Mathf.Max(nodeA, nodeB);

        foreach (var p in paths)
        {
            if (p.from == min && p.to == max)
                return p;
        }
        return null;
    }

    private void UpdateRouteVisuals()
    {
        foreach (var p in paths)
        {
            p.pathObject.SetActive(false);
            UnhighlightPath(p);
        }

        currentPath = GetPathBetween(currentNodeIndex, nextNodeIndex);
        nextPath = GetPathBetween(nextNodeIndex, upcomingNodeIndex);

        if (currentPath != null)
        {
            currentPath.pathObject.SetActive(true);
            HighlightPath(currentPath);
        }
        if (nextPath != null)
        {
            nextPath.pathObject.SetActive(true);
            HighlightPath(nextPath);
        }
    }

    public Vector3 GetCurrentPathForwardVector()
    {
        if (currentPath != null && currentPath.pathObject != null)
        {
            return currentPath.pathObject.transform.forward;
        }
        return Vector3.zero;
    }
}
