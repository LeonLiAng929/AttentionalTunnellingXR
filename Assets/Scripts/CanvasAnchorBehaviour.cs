using UnityEngine;

public class CanvasAnchorBehaviour : MonoBehaviour
{
    public enum AnchorMode
    {
        Peripersonal,
        Focal,
        Action,
        Ambient
    }

    [Header("Anchor Settings")]
    public AnchorMode currentMode = AnchorMode.Focal;

    [Header("Action Mode Settings")]
    [Tooltip("Distance between the canvas and the user when in Action mode.")]
    public float actionDistance = 5.0f;
    [Tooltip("Speed at which the canvas smoothly follows the user's body rotation and movement.")]
    public float actionSmoothSpeed = 20.0f;

    [Header("Focal Mode Settings")]
    [Tooltip("Fixed distance to float the canvas in front of the user.")]
    public float focalDistance = 1.5f;
    [Tooltip("Speed at which the canvas smoothly follows the user's gaze.")]
    public float focalSmoothSpeed = 20.0f;
    [Tooltip("Offset position (X and Y) relative to the user's gaze in Focal mode.")]
    public Vector2 focalOffset = Vector2.zero;

    public void ApplyModeScale(Vector3 scale)
    {
        transform.localScale = scale;
    }

    void Update()
    {
        if (PlayerTelemetryManager.Instance == null || PlayerTelemetryManager.Instance.centerCam == null) return;

        switch (currentMode)
        {
            case AnchorMode.Action:
                HandleActionMode();
                break;
            case AnchorMode.Focal:
                HandleFocalMode();
                break;
            case AnchorMode.Peripersonal:
                // Intentionally empty. 
                // Since you manually parented this to the Left Controller in the hierarchy, 
                // Unity's transform system handles the movement automatically.
                break;
            case AnchorMode.Ambient:
                // Ambient mode is static once initialized via InitializeAmbient()
                break;
        }
    }

    private void HandleActionMode()
    {
        Transform centerCam = PlayerTelemetryManager.Instance.centerCam;

        // 1. Project centerCam down to y=0 (The user's exact footprint)
        Vector3 groundPosition = new Vector3(centerCam.position.x, 0f, centerCam.position.z);
    
        // 2. Get the user's body rotation (approximated by headset Yaw)
        Quaternion userYawRotation = Quaternion.Euler(0f, centerCam.eulerAngles.y, 0f);

        // 3. Calculate the target position and rotation (where it SHOULD be)
        Vector3 targetPosition = groundPosition + (userYawRotation * Vector3.forward * actionDistance);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.up, userYawRotation * Vector3.forward);

        // 4. Smoothly interpolate from the current position/rotation to the targets
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * actionSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * actionSmoothSpeed);
    }

    private void HandleFocalMode()
    {
        Transform centerCam = PlayerTelemetryManager.Instance.centerCam;

        // Target position: fixed distance directly in front of the user's gaze, with optional offsets
        Vector3 targetPosition = centerCam.position 
                                 + (centerCam.forward * focalDistance)
                                 + (centerCam.right * focalOffset.x)
                                 + (centerCam.up * focalOffset.y);

        // LOGICAL FIX: Target rotation must point from the chart to the camera so it faces the user
        Quaternion targetRotation = Quaternion.LookRotation(centerCam.position - transform.position);

        // Smoothly interpolate position and rotation
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * focalSmoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * focalSmoothSpeed);
    }

    public void InitializeAmbient(Vector3 basePosition, Vector3 lookAtTarget, Vector3 scale, float eyeHeight = 1.7f, float backwardOffset = 0f)
    {
        currentMode = AnchorMode.Ambient;

        if (PlayerTelemetryManager.Instance != null)
        {
            eyeHeight = PlayerTelemetryManager.Instance.calibratedEyeLevel;
        }

        // --- NEW OFFSET LOGIC ---
        // 1. Calculate the flat direction pointing TOWARD the target
        Vector3 directionToTarget = lookAtTarget - basePosition;
        directionToTarget.y = 0f; // Keep the math strictly horizontal
    
        // Normalize to get a pure direction vector (length of 1)
        Vector3 normalizedDir = directionToTarget.normalized;

        // 2. Push the base position BACKWARD along that line of sight
        Vector3 offsetPosition = basePosition - (normalizedDir * backwardOffset);
        
        // Set position using the newly calculated offset position instead of basePosition
        transform.position = new Vector3(offsetPosition.x, eyeHeight, offsetPosition.z);
        
        // Rotation Logic (Y-Axis Only)
        lookAtTarget.y = transform.position.y;
    
        Vector3 finalDirectionToTarget = lookAtTarget - transform.position;
        if (finalDirectionToTarget != Vector3.zero)
        {
            // This already correctly faces the lookAtTarget (the centroid)
            transform.rotation = Quaternion.LookRotation(finalDirectionToTarget);
        }
        
        ApplyModeScale(scale);
    }
}