using UnityEngine;

public class LineChartVisualizer : MonoBehaviour
{
    [Header("Configuration")]
    public Color colorLine1 = Color.orange;
    public Color colorLine2 = Color.blue;
    public GameObject circleMarkerPrefab;
    public GameObject linePrefab;
    private float horizontalPointGapFactor = 1.3f;
    private float physicalChartHeight = 5*1.3f;
    
    [Tooltip("The thickness of the connecting line segments.")]
    public float lineThickness;
    
    [Tooltip("The maximum Y value (used to map data points to the top of the chart area).")]
    private float maxYValue = 15f;

    [Header("Timer UI")]
    public Transform timerBar;
    
    // LOGICAL FIX: 5 points only require 4 connecting line segments
    public GameObject[] lines1 = new GameObject[4];
    public GameObject[] lines2 = new GameObject[4];
    public GameObject[] markersLine1 = new GameObject[5];
    public GameObject[] markersLine2 = new GameObject[5];

    void Awake()
    {
        // Instantiate markers (5 per line)
        for (int i = 0; i < 5; i++)
        {
            GameObject tempm1 = Instantiate(circleMarkerPrefab, transform);
            tempm1.GetComponent<MeshRenderer>().material.color = colorLine1;
            markersLine1[i] = tempm1;
            
            GameObject tempm2 = Instantiate(circleMarkerPrefab, transform);
            tempm2.GetComponent<MeshRenderer>().material.color = colorLine2;
            markersLine2[i] = tempm2;
        }
        
        // Instantiate lines (4 per line)
        for (int i = 0; i < 4; i++)
        {
            GameObject templ1 = Instantiate(linePrefab, transform);
            templ1.GetComponent<MeshRenderer>().material.color = colorLine1;
            lines1[i] = templ1;
            
            GameObject templ2 = Instantiate(linePrefab, transform);
            templ2.GetComponent<MeshRenderer>().material.color = colorLine2;
            lines2[i] = templ2;
        }
        
        lineThickness = .5f;
        linePrefab.SetActive(false); 
        circleMarkerPrefab.SetActive(false);
        
    }

    public void UpdateChart(float[] line1Values, float[] line2Values)
    {

        if (line1Values == null || line1Values.Length != 5 || line2Values == null || line2Values.Length != 5)
        {
            Debug.LogWarning("LineChartVisualizer requires exactly 5 values per line.");
            return;
        }
        
        float horizontalPointGap = horizontalPointGapFactor * markersLine1[0].transform.localScale.x;
        float totalWidth = 4f * horizontalPointGap;
        float startX = -totalWidth / 2f;

        Vector3[] line1Positions = new Vector3[5];
        Vector3[] line2Positions = new Vector3[5];

        // 1. Position the markers and cache coordinates
        for (int i = 0; i < 5; i++)
        {
            float xPos = startX + (i * horizontalPointGap);

            // 0 data value = 0 local Y. Max data value = physicalChartHeight local Y.
            float yPos1 = (line1Values[i]/maxYValue) * physicalChartHeight;
            float yPos2 = (line2Values[i]/maxYValue) * physicalChartHeight;

            Vector3 pos1 = new Vector3(xPos, yPos1, 0.002f);
            Vector3 pos2 = new Vector3(xPos, yPos2, 0f);

            line1Positions[i] = pos1;
            line2Positions[i] = pos2;

            if (markersLine1[i] != null) markersLine1[i].transform.localPosition = pos1;
            if (markersLine2[i] != null) markersLine2[i].transform.localPosition = pos2;
        }

        // 2. Position, Rotate, and Scale the connecting lines
        for (int i = 0; i < 4; i++)
        {
            UpdateLineSegment(lines1[i], line1Positions[i], line1Positions[i + 1]);
            UpdateLineSegment(lines2[i], line2Positions[i], line2Positions[i + 1]);
        }
    }

    /// <summary>
    /// Stretches a 3D mesh between point A and point B using pure Z-axis rotation to prevent scale shearing.
    /// Assumes linePrefab is a standard Unity Cube.
    /// </summary>
    private void UpdateLineSegment(GameObject lineObj, Vector3 pointA, Vector3 pointB)
    {
        if (lineObj == null) return;

        // Center the segment halfway between the two points
        Vector3 midPoint = (pointA + pointB) / 2f;
        lineObj.transform.localPosition = midPoint;

        // Calculate rotation using 2D trig. We only rotate around Z to avoid non-uniform scale skewing.
        float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
        lineObj.transform.localRotation = Quaternion.Euler(0, 0, angle);

        // Scale the X-axis to the distance between points, and cap Y/Z to the thickness
        float distance = Vector3.Distance(pointA, pointB);
        lineObj.transform.localScale = new Vector3(distance, lineThickness, lineThickness);
    }

    public void StartTimer(float duration)
    {
        if (timerBar == null) return;
        timerBar.localScale = new Vector3(horizontalPointGapFactor*5f, .2f, .2f);
        LeanTween.scaleX(timerBar.gameObject, 0f, duration);
    }

    public void ResetTimer()
    {
        if (timerBar == null) return;
        LeanTween.cancel(timerBar.gameObject);
        timerBar.localScale = new Vector3(horizontalPointGapFactor*5f, .2f, .2f);
    }
}
