using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Meta.XR.BuildingBlocks;
using TMPro;

public class AnchorTest : MonoBehaviour
{
    [System.Serializable]
    public class GymAnchorData
    {
        public List<string> anchorUuids = new List<string>();
    }

    [Header("Core Dependencies")]
    [Tooltip("Reference to the SpatialAnchorCoreBuildingBlock.")]
    [SerializeField] private SpatialAnchorCoreBuildingBlock anchorCore;
    [Tooltip("The prefab that will be spawned at the anchor's location.")]
    [SerializeField] private GameObject anchorPrefab;

    [Header("Tracking Alignment")]
    [Tooltip("Drag the RightControllerAnchor from inside your OVRCameraRig here to get true World Position.")]
    [SerializeField] private Transform rightControllerTransform;
    [Tooltip("Place ALL your virtual study objects inside this parent. It will snap to the Origin Anchor.")]
    [SerializeField] private Transform virtualEnvironmentRoot;

    [Header("UI Debugging")]
    public TMP_Text debugText;

    private string saveFilePath;
    private GymAnchorData savedData = new GymAnchorData();
    private Guid currentSessionAnchorUuid;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "gym_anchors.json");
    }

    private void OnEnable()
    {
        if (anchorCore != null)
        {
            anchorCore.OnAnchorCreateCompleted.AddListener(HandleAnchorCreated);
            anchorCore.OnAnchorEraseCompleted.AddListener(HandleAnchorErased);
            // CRITICAL: Subscribe to the load completion event
            anchorCore.OnAnchorsLoadCompleted.AddListener(HandleAnchorsLoaded);
            anchorCore.OnAnchorsEraseAllCompleted.AddListener(HandleAllAnchorsErased);
        }
    }

    private void OnDisable()
    {
        if (anchorCore != null)
        {
            anchorCore.OnAnchorCreateCompleted.RemoveListener(HandleAnchorCreated);
            anchorCore.OnAnchorEraseCompleted.RemoveListener(HandleAnchorErased);
            anchorCore.OnAnchorsLoadCompleted.RemoveListener(HandleAnchorsLoaded);
            anchorCore.OnAnchorsEraseAllCompleted.RemoveListener(HandleAllAnchorsErased);
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            // FIX 1: Use true World Position and Rotation from the controller's transform
            if (rightControllerTransform != null)
            {
                CreateGymAnchor(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            else
            {
                LogDebug("Error: Assign the RightControllerAnchor in the Inspector!");
            }
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.B))
        {
            EraseCurrentAnchor();
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.RThumbstick))
        {
            LoadAnchorsFromDisk();
        }
        else if (OVRInput.GetDown(OVRInput.RawButton.LThumbstick)) // NEW: Left Controller Y Button
        {
            EraseAllGymAnchors(); // Wipes the whole gym
        }
    }

    
    /// <summary>
    /// Calls the building block to erase all instantiated anchors from the device's tracking registry.
    /// </summary>
    public void EraseAllGymAnchors()
    {
        LogDebug("Initiating erasure of ALL anchors...");
        anchorCore.EraseAllAnchors();
    }

    /// <summary>
    /// Callback triggered when the building block finishes the Erase All operation.
    /// </summary>
    private void HandleAllAnchorsErased(OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            LogDebug("[Success] All anchors erased from OS. Clearing JSON data...");

            // CRITICAL: Synchronize the application data with the OS
            savedData.anchorUuids.Clear();
            string json = JsonUtility.ToJson(savedData, true);
            File.WriteAllText(saveFilePath, json);

            currentSessionAnchorUuid = Guid.Empty;
            LogDebug("Gym environment reset. All anchor data wiped completely.");
        }
        else
        {
            LogDebug($"[Failure] Failed to erase all anchors: {result}");
        }
    }
    public void CreateGymAnchor(Vector3 position, Quaternion rotation)
    {
        LogDebug("Initiating anchor creation...");
        anchorCore.InstantiateSpatialAnchor(anchorPrefab, position, rotation);
    }

    private void HandleAnchorCreated(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            currentSessionAnchorUuid = anchor.Uuid;
            LogDebug($"[Success] Anchor created! UUID: {currentSessionAnchorUuid}");
            SaveUuidToFile(currentSessionAnchorUuid);
        }
        else
        {
            LogDebug($"[Failure] Anchor creation failed: {result}");
        }
    }

    public void EraseCurrentAnchor()
    {
        if (currentSessionAnchorUuid != Guid.Empty)
        {
            LogDebug($"Initiating erasure for UUID: {currentSessionAnchorUuid}...");
            anchorCore.EraseAnchorByUuid(currentSessionAnchorUuid);
        }
    }

    private void HandleAnchorErased(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
    {
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            LogDebug($"[Success] Anchor {anchor.Uuid} erased.");
            currentSessionAnchorUuid = Guid.Empty;
        }
        else
        {
            LogDebug($"[Failure] Failed to erase anchor: {result}");
        }
    }

    public void SaveUuidToFile(Guid newUuid)
    {
        // SAFETY PATCH: Load existing data first to prevent overwriting an empty memory list over a full file.
        if (File.Exists(saveFilePath) && savedData.anchorUuids.Count == 0)
        {
            string existingJson = File.ReadAllText(saveFilePath);
            savedData = JsonUtility.FromJson<GymAnchorData>(existingJson);
        }

        string uuidString = newUuid.ToString();
        if (!savedData.anchorUuids.Contains(uuidString))
        {
            savedData.anchorUuids.Add(uuidString);
        }

        string json = JsonUtility.ToJson(savedData, true);
        File.WriteAllText(saveFilePath, json);
        LogDebug($"Saved UUID. Total tracked: {savedData.anchorUuids.Count}");
    }

    public void LoadAnchorsFromDisk()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            savedData = JsonUtility.FromJson<GymAnchorData>(json);

            List<Guid> uuidsToLoad = new List<Guid>();
            foreach (string uuidString in savedData.anchorUuids)
            {
                if (Guid.TryParse(uuidString, out Guid parsedGuid))
                {
                    uuidsToLoad.Add(parsedGuid);
                }
            }

            if (uuidsToLoad.Count > 0)
            {
                LogDebug($"Loading {uuidsToLoad.Count} anchors...");
                anchorCore.LoadAndInstantiateAnchors(anchorPrefab, uuidsToLoad);
            }
        }
        else
        {
            LogDebug("No saved anchor data found.");
        }
    }

    // FIX 2: Environment Alignment upon successful load
    private void HandleAnchorsLoaded(List<OVRSpatialAnchor> loadedAnchors)
    {
        if (loadedAnchors.Count > 0 && virtualEnvironmentRoot != null)
        {
            // We treat the very first anchor in the list as the "Origin Anchor"
            OVRSpatialAnchor originAnchor = loadedAnchors[0];

            // Snap the virtual environment to match the physical gym's anchor
            virtualEnvironmentRoot.SetPositionAndRotation(originAnchor.transform.position, originAnchor.transform.rotation);
            
            LogDebug("Anchors loaded. Virtual Environment aligned to Origin Anchor.");
        }
        else if (virtualEnvironmentRoot == null)
        {
            LogDebug("Warning: Anchors loaded, but no Virtual Environment Root assigned to align!");
        }
    }

    // Helper method to keep UI and Console logging unified
    private void LogDebug(string message)
    {
        Debug.Log(message);
        if (debugText != null)
        {
            debugText.text = message;
        }
    }
}