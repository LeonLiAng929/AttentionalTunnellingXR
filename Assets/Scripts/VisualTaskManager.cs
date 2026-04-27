using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VisualTaskManager : MonoBehaviour
{
    public static VisualTaskManager instance;
    [Header("Configuration")]
    public CanvasAnchorBehaviour.AnchorMode currentStudyMode = CanvasAnchorBehaviour.AnchorMode.Focal;
    //public GameObject visPrefab;
    public float updateInterval = 3.0f;
    public float maxGeneratedDataValue = 15f;

    // Object Pooling
    public GameObject peripersonalCanvas;
    //public Vector3 periperonalScale;
    public GameObject actionCanvas;
    //public Vector3 actionScale;
    public GameObject focalCanvas;
    //public Vector3 focalScale;
    [SerializeField]
    public List<GameObject> ambientCanvases = new List<GameObject>();
    public Transform ambientCanvasesContainer;
    public Vector3 ambientScale;
    public float  ambientBackwardOffset = 1.5f;
    public GameObject visPrefab;

    private List<LineChartVisualizer> activeVisualizers = new List<LineChartVisualizer>();

    private float timer = 0f;
    private int correctTargetLine = 1;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        PreInstantiateCanvases();
    }

    private void PreInstantiateCanvases()
{
    // Instantiate Ambient canvases
    if (StudyManager.instance != null)
    {
        List<Transform> anchorList = StudyManager.instance.debugAnchorList;
        
        if (anchorList != null && anchorList.Count > 0)
        {
            // Calculate centroid
            Vector3 centroid = Vector3.zero;
            foreach (Transform t in anchorList)
            {
                centroid += t.position;
            }
            centroid /= anchorList.Count;

            // Anchor Canvases
            /*foreach (Transform anchor in anchorList)
            {
                GameObject ambientObj = Instantiate(visPrefab, ambientCanvasesContainer);
                CanvasAnchorBehaviour cab = ambientObj.GetComponent<CanvasAnchorBehaviour>();
                if (cab != null) cab.InitializeAmbient(anchor.position, centroid, ambientScale, backwardOffset: ambientBackwardOffset);
                ambientCanvases.Add(ambientObj);
            }*/

            // Midpoint Canvases (Based on adjacent anchors)
            // We loop through the anchors to place a canvas between each consecutive pair
            for (int i = 0; i < anchorList.Count; i++)
            {
                // The modulo (%) wraps the index back to 0 when it reaches the end of the list.
                // This connects the final anchor back to the first anchor to close the physical loop.
                int nextIndex = (i + 1) % anchorList.Count;

                Vector3 p1 = anchorList[i].position;
                Vector3 p2 = anchorList[nextIndex].position;
                Vector3 midpoint = (p1 + p2) / 2f;
                
                // Treat the centroid as the focal lookAtTarget to face the center of the room
                GameObject ambientObj = Instantiate(visPrefab, ambientCanvasesContainer);
                CanvasAnchorBehaviour cab = ambientObj.GetComponent<CanvasAnchorBehaviour>();
                cab.InitializeAmbient(midpoint, centroid, ambientScale, backwardOffset: ambientBackwardOffset);
                ambientCanvases.Add(ambientObj);
            }
        }
    }

    SetMode();
    // Deactivate all to start cleanly
    //DeactivateAllCanvases();
}


    public void SetMode()
    {
        switch (currentStudyMode)
        {
            case  CanvasAnchorBehaviour.AnchorMode.Peripersonal:
                peripersonalCanvas.SetActive(true);
                actionCanvas.SetActive(false);
                focalCanvas.SetActive(false);
                ambientCanvases.ForEach(ac => ac.SetActive(false));
                break;
            case   CanvasAnchorBehaviour.AnchorMode.Focal:
                peripersonalCanvas.SetActive(false);
                actionCanvas.SetActive(false);
                focalCanvas.SetActive(true);
                ambientCanvases.ForEach(ac => ac.SetActive(false));
                break;
            case   CanvasAnchorBehaviour.AnchorMode.Ambient:
                peripersonalCanvas.SetActive(false);
                actionCanvas.SetActive(false);
                focalCanvas.SetActive(false);
                ambientCanvases.ForEach(ac => ac.SetActive(true));
                break;
            case CanvasAnchorBehaviour.AnchorMode.Action:
                peripersonalCanvas.SetActive(false);
                actionCanvas.SetActive(true);
                focalCanvas.SetActive(false);
                ambientCanvases.ForEach(ac => ac.SetActive(false));
                break;
        }
        
        activeVisualizers.Clear();
        if (currentStudyMode == CanvasAnchorBehaviour.AnchorMode.Peripersonal && peripersonalCanvas != null) activeVisualizers.Add(peripersonalCanvas.GetComponent<LineChartVisualizer>());
        else if (currentStudyMode == CanvasAnchorBehaviour.AnchorMode.Action && actionCanvas != null) activeVisualizers.Add(actionCanvas.GetComponent<LineChartVisualizer>());
        else if (currentStudyMode == CanvasAnchorBehaviour.AnchorMode.Focal && focalCanvas != null) activeVisualizers.Add(focalCanvas.GetComponent<LineChartVisualizer>());
        else if (currentStudyMode == CanvasAnchorBehaviour.AnchorMode.Ambient)
        {
            foreach (var ac in ambientCanvases) 
            {
                if (ac != null) activeVisualizers.Add(ac.GetComponent<LineChartVisualizer>());
            }
        }

        // Ensure no nulls snuck in
        activeVisualizers.RemoveAll(v => v == null);

        // Immediately push data to the newly activated charts
        GenerateNewTrial();
    }
    private void DeactivateAllCanvases()
    {
        if (peripersonalCanvas != null) peripersonalCanvas.SetActive(false);
        if (actionCanvas != null) actionCanvas.SetActive(false);
        if (focalCanvas != null) focalCanvas.SetActive(false);

        foreach (var ac in ambientCanvases)
        {
            if (ac != null) ac.SetActive(false);
        }
    }


    private void GenerateNewTrial()
    {
        float[] line1 = new float[5];
        float[] line2 = new float[5];
        bool uniqueMaxFound = false;

        // Loop until we generate a set of numbers with exactly ONE unique maximum
        while (!uniqueMaxFound)
        {
            int maxVal = -1;
            int maxCount = 0;

            // 1. Generate integer values (we cast them to float arrays so LineChartVisualizer doesn't complain)
            for (int i = 0; i < 5; i++)
            {
                // Random.Range with integers is exclusive at the top end, so we add 1
                int intMax = (int)maxGeneratedDataValue + 1;
                line1[i] = Random.Range(0, intMax);
                line2[i] = Random.Range(0, intMax);
            }

            // 2. Find the absolute maximum value
            for (int i = 0; i < 5; i++)
            {
                if (line1[i] > maxVal) maxVal = (int)line1[i];
                if (line2[i] > maxVal) maxVal = (int)line2[i];
            }

            // 3. Count how many times this maximum occurs across all 10 points
            for (int i = 0; i < 5; i++)
            {
                if ((int)line1[i] == maxVal) maxCount++;
                if ((int)line2[i] == maxVal) maxCount++;
            }

            // 4. If the maximum only exists in one spot, we have a valid trial!
            if (maxCount == 1)
            {
                uniqueMaxFound = true;

                // 5. Determine which line holds the winning value
                for (int i = 0; i < 5; i++)
                {
                    if ((int)line1[i] == maxVal) correctTargetLine = 1;
                    if ((int)line2[i] == maxVal) correctTargetLine = 2;
                }
            }
        }

        // Push the new data to the active canvases
        foreach (var visualizer in activeVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.UpdateChart(line1, line2);
                visualizer.ResetTimer();
                visualizer.StartTimer(updateInterval);
            }
        }

        timer = updateInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            GenerateNewTrial();
        }

        // Listen for user guess
        bool guessedLine1 = OVRInput.GetDown(OVRInput.RawButton.X);
        bool guessedLine2 = OVRInput.GetDown(OVRInput.RawButton.A);

        if (guessedLine1 || guessedLine2)
        {
            int guess = guessedLine1 ? 1 : 2;
            if (guess == correctTargetLine)
            {
                Debug.Log($"<color=green>Visual Task: Correct! (Guessed {guess})</color>");
            }
            else
            {
                Debug.Log($"<color=red>Visual Task: Incorrect! (Guessed {guess}, Truth was {correctTargetLine})</color>");
            }
            
            GenerateNewTrial();
        }
    }

    void OnDestroy()
    {
        foreach (var visualizer in activeVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.ResetTimer();
            }
        }
    }
    
    void OnDisable()
    {
        foreach (var visualizer in activeVisualizers)
        {
            if (visualizer != null)
            {
                visualizer.ResetTimer();
            }
        }
    }
}
