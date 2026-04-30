using UnityEngine;

public class SpatialAligner : MonoBehaviour
{
    public static SpatialAligner Instance { get; private set; }

    public Vector3 originPosition { get; private set; } = Vector3.zero;
    public Vector3 forwardVector { get; private set; } = Vector3.forward;

    private Quaternion alignmentRotation = Quaternion.identity;
    private Quaternion inverseAlignmentRotation = Quaternion.identity;

    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void SetAnchors(Transform a0, Transform a1)
    {
        if (a0 == null || a1 == null)
        {
            Debug.LogError("[SpatialAligner] Anchors are null, cannot align.");
            return;
        }

        // 1. Establish a0.position as the origin (0,0,0), strictly ignoring height
        originPosition = new Vector3(a0.position.x, 0f, a0.position.z);

        // 2. Calculate horizontal vector from a0 to a1 to define strict Forward (+Z)
        Vector3 rawForward = a1.position - a0.position;
        rawForward.y = 0f; // ignore Y values

        if (rawForward.sqrMagnitude < 0.001f)
        {
            Debug.LogError("[SpatialAligner] Anchors are too close or aligned vertically. Falling back to default forward.");
            forwardVector = Vector3.forward;
        }
        else
        {
            forwardVector = rawForward.normalized;
        }

        // Calculate the rotation required to align the raw forward with the world's Vector3.forward
        alignmentRotation = Quaternion.LookRotation(forwardVector, Vector3.up);
        inverseAlignmentRotation = Quaternion.Inverse(alignmentRotation);

        isInitialized = true;
        Debug.Log($"<color=green>[SpatialAligner] Initialized. Origin: {originPosition}, Forward: {forwardVector}</color>");

        // Sanity Check for a1
        Vector3 alignedA1Pos = GetAlignedPosition(a1.position);
        Vector3 alignedA1Forward = GetAlignedRotation(a1.rotation) * Vector3.forward;
        Debug.Log($"<color=yellow>[SpatialAligner] Sanity Check -> a1 Aligned Pos: {alignedA1Pos}, a1 Aligned Forward: {alignedA1Forward}</color>");
    }

    public Vector3 GetAlignedPosition(Vector3 rawPos)
    {
        if (!isInitialized) return rawPos;

        // 1. Translate point so origin is at (0,0,0)
        Vector3 localPos = rawPos - originPosition;

        // 2. Rotate point so that the standard forward aligns with Z-axis
        return inverseAlignmentRotation * localPos;
    }

    public Quaternion GetAlignedRotation(Quaternion rawRot)
    {
        if (!isInitialized) return rawRot;

        // Rotate the rotation to match the standardized coordinate system
        return inverseAlignmentRotation * rawRot;
    }

    public Vector2 GetFOVAngles(Vector3 hazardPos, Transform centerCam)
    {
        if (centerCam == null) return Vector2.zero;

        // Vector from camera to hazard
        Vector3 directionToHazard = hazardPos - centerCam.position;

        // Convert that direction to the camera's local space
        Vector3 localDirection = centerCam.InverseTransformDirection(directionToHazard);

        // Calculate horizontal angle (yaw) using Atan2 on X and Z
        float horizontalAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        // Calculate vertical angle (pitch) using Atan2 on Y and length in XZ plane
        float horizontalDistance = new Vector2(localDirection.x, localDirection.z).magnitude;
        float verticalAngle = Mathf.Atan2(localDirection.y, horizontalDistance) * Mathf.Rad2Deg;

        return new Vector2(horizontalAngle, verticalAngle);
    }
}
