using UnityEngine;
using System.Collections.Generic;

public class PlayerTelemetryManager : MonoBehaviour
{
    public static PlayerTelemetryManager Instance { get; private set; }

    public Transform centerCam;

    [Header("Calibration Data")]
    [Tooltip("The calibrated Y-axis value of the headset.")]
    public float calibratedEyeLevel = 0f;

    [Header("Telemetry Data")]
    [Tooltip("Average movement speed over the last 3 seconds (units/sec).")]
    public float averageSpeed = 0f;
    
    [Tooltip("Normalized directional vector representing the user's path of travel over the last 3 seconds (projected on XZ plane).")]
    public Vector3 movementVector = Vector3.zero;

    private struct PositionFrame
    {
        public Vector3 position;
        public float time;
    }

    // Using a Queue as an efficient rolling buffer for the last 3 seconds of position frames
    private Queue<PositionFrame> positionBuffer = new Queue<PositionFrame>();
    private const float BUFFER_DURATION = 3f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        HandleCalibration();
        UpdateTelemetry();
    }

    private void HandleCalibration()
    {
        // Monitor both left and right grip triggers simultaneously
        bool leftGrip = OVRInput.Get(OVRInput.RawButton.LHandTrigger);
        bool rightGrip = OVRInput.Get(OVRInput.RawButton.RHandTrigger);

        if (leftGrip && rightGrip)
        {
            if (centerCam != null)
            {
                calibratedEyeLevel = centerCam.position.y;
                // Optional: You can add a debug log or notification here to confirm calibration visually
                // Debug.Log($"[PlayerTelemetryManager] Eye level calibrated to: {calibratedEyeLevel}");
            }
        }
    }

    private void UpdateTelemetry()
    {
        if (centerCam == null) return;

        // Capture current headset position and time
        Vector3 currentPosition = centerCam.position;
        float currentTime = Time.time;

        positionBuffer.Enqueue(new PositionFrame { position = currentPosition, time = currentTime });

        // Remove old frames that are outside the 3-second window
        while (positionBuffer.Count > 0 && positionBuffer.Peek().time < currentTime - BUFFER_DURATION)
        {
            positionBuffer.Dequeue();
        }

        if (positionBuffer.Count >= 2)
        {
            PositionFrame oldestFrame = positionBuffer.Peek();
            float timeDifference = currentTime - oldestFrame.time;

            if (timeDifference > 0)
            {
                // Calculate Displacement (Oldest to Newest)
                Vector3 displacement = currentPosition - oldestFrame.position;

                // We calculate average speed based on the accumulated path length over the buffer 
                // to account for curves in the run, rather than just straight-line displacement.
                float totalPathLength = 0f;
                Vector3 prevPos = oldestFrame.position;
                
                foreach (var frame in positionBuffer)
                {
                    totalPathLength += Vector3.Distance(prevPos, frame.position);
                    prevPos = frame.position;
                }

                averageSpeed = totalPathLength / timeDifference;

                // Movement Vector should typically represent the overall direction of travel (decoupled from head look).
                // We use the start and end of the 3-second buffer, flattened on the XZ plane to ignore head bobbing.
                Vector3 flatDisplacement = new Vector3(displacement.x, 0, displacement.z);
                
                if (flatDisplacement.sqrMagnitude > 0.001f)
                {
                    movementVector = flatDisplacement.normalized;
                }
                else
                {
                    movementVector = Vector3.zero;
                }
            }
        }
        else
        {
            // Reset to zero if there's insufficient data (e.g. at the very start)
            averageSpeed = 0f;
            movementVector = Vector3.zero;
        }
    }
}
