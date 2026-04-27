using UnityEngine;
using System.Collections.Generic;

public class HazardSpawner : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject hazardPrefab;
    [Tooltip("Exact distance from the user to spawn the hazard.")]
    public float spawnDistance = 10f;
    
    [Header("Trial Quotas")]
    public int maxStaticHazards = 5;
    public int maxSlowHazards = 5;
    public int maxFastHazards = 5;

    [Header("References")]
    public StudyManager studyManager;

    // Counters
    private int spawnedStatic = 0;
    private int spawnedSlow = 0;
    private int spawnedFast = 0;

    private float timer = 0f;
    private float currentSpawnInterval = 0f;
    private GameObject currentHazardInstance = null;
    
    private bool isPhaseComplete = false;

    void Start()
    {
        if (studyManager == null)
        {
            studyManager = FindObjectOfType<StudyManager>();
        }

        SetNewRandomInterval();
    }

    void Update()
    {
        HandleDestructionLifecycle();

        if (isPhaseComplete) return;

        timer += Time.deltaTime;
        
        if (timer >= currentSpawnInterval)
        {
            if (AreAllQuotasMet())
            {
                isPhaseComplete = true;
                return;
            }

            AttemptSpawn();
        }
    }

    private void SetNewRandomInterval()
    {
        currentSpawnInterval = Random.Range(4.0f, 10.0f);
        timer = 0f;
    }

    private void HandleDestructionLifecycle()
    {
        // Check for Index Trigger input on either controller
        bool indexTriggerPressed = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger) || OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);

        if (currentHazardInstance != null && indexTriggerPressed)
        {
            // Destroy immediately. Do not reset timer; wait for interval to complete.
            Destroy(currentHazardInstance);
            currentHazardInstance = null;
        }
    }

    private bool AreAllQuotasMet()
    {
        return spawnedStatic >= maxStaticHazards && 
               spawnedSlow >= maxSlowHazards && 
               spawnedFast >= maxFastHazards;
    }

    /// <summary>
    /// Randomly selects a profile that has not yet reached its quota.
    /// Returns false if all profiles are exhausted.
    /// </summary>
    private bool TrySelectProfile(out HazardBehaviour.SpeedProfile selectedProfile)
    {
        List<HazardBehaviour.SpeedProfile> availableProfiles = new List<HazardBehaviour.SpeedProfile>();

        if (spawnedStatic < maxStaticHazards) availableProfiles.Add(HazardBehaviour.SpeedProfile.Static);
        if (spawnedSlow < maxSlowHazards) availableProfiles.Add(HazardBehaviour.SpeedProfile.Slow);
        if (spawnedFast < maxFastHazards) availableProfiles.Add(HazardBehaviour.SpeedProfile.Fast);

        if (availableProfiles.Count == 0)
        {
            selectedProfile = HazardBehaviour.SpeedProfile.Static; // Default, won't be used
            return false;
        }

        selectedProfile = availableProfiles[Random.Range(0, availableProfiles.Count)];
        return true;
    }

    private void AttemptSpawn()
    {
        if (studyManager == null || PlayerTelemetryManager.Instance == null || PlayerTelemetryManager.Instance.centerCam == null) return;

        Vector3 userPos = PlayerTelemetryManager.Instance.centerCam.position;
        Vector3 pathForward = studyManager.GetCurrentPathForwardVector();
        
        if (pathForward == Vector3.zero) return; // No active path to reference

        // 1. Pick a random angle within a 110-degree arc (-55 to +55 degrees)
        float randomAngle = Random.Range(-55f, 55f);
        
        // 2. Rotate the path's forward vector around the Y axis by the random angle
        Vector3 spawnDirection = Quaternion.Euler(0, randomAngle, 0) * pathForward;

        // 3. Calculate preliminary spawn position exactly spawnDistance away
        Vector3 spawnPos = userPos + (spawnDirection * spawnDistance);

        // 4. Set exact Y level to the calibrated eye level
        spawnPos.y = PlayerTelemetryManager.Instance.calibratedEyeLevel;

        // 5. Check if inside established spatial arena bounds
        if (!IsInsideArena(spawnPos))
        {
            // Postpone spawn, evaluate again next frame
            return;
        }

        // We have a valid position. Get a non-exhausted profile.
        if (TrySelectProfile(out HazardBehaviour.SpeedProfile profile))
        {
            // Destroy any lingering old hazard that was ignored
            if (currentHazardInstance != null)
            {
                Destroy(currentHazardInstance);
                currentHazardInstance = null;
            }

            // Spawn the hazard
            GameObject hazardObj = Instantiate(hazardPrefab, spawnPos, Quaternion.identity);
            currentHazardInstance = hazardObj;

            HazardBehaviour hazardBehaviour = hazardObj.GetComponent<HazardBehaviour>();
            if (hazardBehaviour != null)
            {
                hazardBehaviour.Initialize(profile, userPos, PlayerTelemetryManager.Instance.averageSpeed, pathForward);
            }

            // Increment the appropriate tracking quota
            if (profile == HazardBehaviour.SpeedProfile.Static) spawnedStatic++;
            else if (profile == HazardBehaviour.SpeedProfile.Slow) spawnedSlow++;
            else if (profile == HazardBehaviour.SpeedProfile.Fast) spawnedFast++;

            // Restart the timer since we successfully spawned one
            SetNewRandomInterval();
        }
        else
        {
            isPhaseComplete = true; // Safety check in case quotas filled mid-frame
        }
    }

    /// <summary>
    /// Checks if a position is within the established bounds of the spatial arena.
    /// This uses the min/max XZ bounds of DimensionVisualiser.instance.anchorList.
    /// </summary>
    private bool IsInsideArena(Vector3 position)
    {
        //if (DimensionVisualiser.instance == null || DimensionVisualiser.instance.anchorList == null || DimensionVisualiser.instance.anchorList.Count == 0) return true;

        List<Transform> anchorList = studyManager.debugAnchorList;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var anchor in anchorList)
        {
            Vector3 p = anchor.transform.position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        // Adding a 2.0 meter padding to allow hazards to spawn slightly outside the node boundary box
        float padding = 2.0f;

        return position.x >= (minX - padding) && position.x <= (maxX + padding) &&
               position.z >= (minZ - padding) && position.z <= (maxZ + padding);
    }
}
