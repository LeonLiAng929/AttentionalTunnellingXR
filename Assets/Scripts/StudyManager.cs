using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StudyManager : MonoBehaviour
{
    public static StudyManager instance;
    public GameObject pathPrefab;
    public Material highlightMaterial;
    public Material defaultMaterial;
    /*public enum Mode
    {
        SetupMode,
        StudyMode
    }
    
    public Mode currMode = Mode.SetupMode;*/
    public List<Path> paths = new List<Path>();
    
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

    // Update is called once per frame
    void Update()
    {
        
    }

    /*public void ChangeMode()
    {
        currMode = currMode == Mode.SetupMode ? Mode.StudyMode : Mode.SetupMode;
        HelperScript.Instance.ShowNotification("Current Mode: " + currMode);
        
    }*/

    public void PathMapping()
    {
        List<OVRSpatialAnchor> anchorList = DimensionVisualiser.instance.anchorList;
        ClearPaths();
     
        for (int i = 0; i < anchorList.Count; i++)
        {
            for (int j = i + 1; j < anchorList.Count; j++)
            {
                Vector3 pointA = anchorList[i].transform.position;
                Vector3 pointB = anchorList[j].transform.position;


                pointA.y = 0;
                pointB.y = 0;
                Vector3 midPoint = (pointA + pointB) / 2f;
                Vector3 heading = pointB - pointA; 
                float distance = heading.magnitude; 
                Vector3 direction = heading.normalized;
                Quaternion rotation = Quaternion.LookRotation(direction);
                
                GameObject temp = Instantiate(pathPrefab, midPoint, rotation, anchorList[j].transform);
                foreach (Transform t in temp.GetComponentsInChildren<Transform>())
                {
                    t.localScale = new Vector3(t.localScale.x, t.localScale.y, distance);
                }
                Path p = new Path(i, j, temp);
                
                paths.Add(p);
            }
        }
    }

    public void ClearPaths()
    {
        foreach (Path p in paths)
        {
            Destroy(p.pathObject);
        }
        paths.Clear();
    }
}
