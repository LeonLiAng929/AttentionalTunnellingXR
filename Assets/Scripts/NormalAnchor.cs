using System;
using Meta.XR.BuildingBlocks;
using UnityEngine;

public class NormalAnchor : MonoBehaviour
{
    public SpatialAnchorCoreBuildingBlock spatialAnchorCore;
    public SpatialAnchorSpawnerBuildingBlock spatialAnchorSpawner;
    public SpatialAnchorLoaderBuildingBlock spatialAnchorLoader;
    /// <summary>
    /// A prefab to instantiate.
    /// </summary>
    public GameObject AnchorPrefab;


    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            position.y = 0.5f;
            spatialAnchorSpawner.SpawnSpatialAnchor(position, Quaternion.identity);
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            spatialAnchorCore.EraseAllAnchors();
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.RThumbstickRight))
        {
            spatialAnchorLoader.LoadAnchorsFromDefaultLocalStorage();
        }
        //spatialAnchorCoreBuildingBlock.OnAnchorCreateCompleted.
    }
   
}

