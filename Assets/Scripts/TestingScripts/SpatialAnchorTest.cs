using UnityEngine;

public class SpatialAnchorTest : MonoBehaviour
{
    public GameObject spatialAnchorPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            CreateSpatialAnchor();
        }
    }

    public void CreateSpatialAnchor()
    {
        Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        position.y = 0.5f;
        GameObject prefab = Instantiate(spatialAnchorPrefab, position, Quaternion.identity);
        prefab.AddComponent<OVRSpatialAnchor>();
    }
}
