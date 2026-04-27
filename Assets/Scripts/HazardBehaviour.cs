using UnityEngine;

public class HazardBehaviour : MonoBehaviour
{
    public enum SpeedProfile
    {
        Static, // 0 km/h
        Slow,   // 4.5 km/h
        Fast    // 9.0 km/h
    }

    [Header("Hazard Settings")]
    public SpeedProfile currentProfile = SpeedProfile.Static;
    
    private float moveSpeed = 0f; // in meters per second
    private Vector3 lockedTrajectoryDirection = Vector3.zero;
    private bool isInitialized = false;

    // Speeds in m/s (km/h divided by 3.6)
    private const float SPEED_STATIC = 0f;
    private const float SPEED_SLOW = 4.5f / 3.6f; // 1.25 m/s
    private const float SPEED_FAST = 9.0f / 3.6f; // 2.5 m/s

    /// <summary>
    /// Initializes the hazard and calculates its interception trajectory.
    /// </summary>
    /// <param name="profile">The speed profile to use.</param>
    /// <param name="userPosition">The current position of the user.</param>
    /// <param name="userSpeed">The user's average speed in m/s.</param>
    /// <param name="pathForwardVector">The normalized directional vector of the path the user is moving along.</param>
    public void Initialize(SpeedProfile profile, Vector3 userPosition, float userSpeed, Vector3 pathForwardVector)
    {
        currentProfile = profile;

        // 1. Assign speed based on profile
        switch (currentProfile)
        {
            case SpeedProfile.Static:
                moveSpeed = SPEED_STATIC;
                break;
            case SpeedProfile.Slow:
                moveSpeed = SPEED_SLOW;
                break;
            case SpeedProfile.Fast:
                moveSpeed = SPEED_FAST;
                break;
        }

        Vector3 hazardSpawnPosition = transform.position;

        // 2. Trajectory Calculation
        if (moveSpeed <= 0.001f)
        {
            // Static hazard doesn't need a trajectory, but we give it a default forward vector
            lockedTrajectoryDirection = transform.forward;
        }
        else
        {
            lockedTrajectoryDirection = CalculateInterceptionDirection(
                hazardSpawnPosition, moveSpeed, 
                userPosition, userSpeed, pathForwardVector.normalized
            );

            // Optional: Face the trajectory direction
            if (lockedTrajectoryDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lockedTrajectoryDirection);
            }
        }

        isInitialized = true;
    }

    void Update()
    {
        // 3. Execution: Move continuously along the locked trajectory
        if (isInitialized && moveSpeed > 0f)
        {
            transform.position += lockedTrajectoryDirection * (moveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Calculates the optimal direction to intercept the moving user.
    /// Uses quadratic equation to solve for interception time.
    /// </summary>
    private Vector3 CalculateInterceptionDirection(Vector3 pHazard, float sHazard, Vector3 pUser, float sUser, Vector3 vUser)
    {
        // Relative position from Hazard to User
        Vector3 R = pUser - pHazard;

        // If the user isn't moving, just move straight to them
        if (sUser <= 0.001f)
        {
            return R.normalized;
        }

        // We solve for t in: |pHazard + dHazard * sHazard * t| = |pUser + vUser * sUser * t|
        // This reduces to a quadratic equation: a*t^2 + b*t + c = 0
        float a = (sUser * sUser) - (sHazard * sHazard);
        float b = 2f * Vector3.Dot(R, vUser) * sUser;
        float c = R.sqrMagnitude;

        float t = -1f; // The time of interception

        if (Mathf.Abs(a) < 0.0001f)
        {
            // Hazard and user have the exact same speed
            if (b < 0f) 
            {
                t = -c / b;
            }
        }
        else
        {
            float discriminant = (b * b) - (4f * a * c);

            if (discriminant >= 0f)
            {
                // Real roots exist, interception is mathematically possible
                float t1 = (-b + Mathf.Sqrt(discriminant)) / (2f * a);
                float t2 = (-b - Mathf.Sqrt(discriminant)) / (2f * a);

                // We want the smallest positive time
                if (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
                else if (t1 > 0f) t = t1;
                else if (t2 > 0f) t = t2;
            }
        }

        // If t > 0, perfect interception is possible
        if (t > 0f)
        {
            Vector3 interceptPoint = pUser + (vUser * sUser * t);
            return (interceptPoint - pHazard).normalized;
        }
        else
        {
            // Interception is impossible (hazard is slower and user is moving away).
            // Calculate trajectory for the closest possible point of approach.
            // The closest the user will ever get to the hazard's spawn point:
            float tClosest = -Vector3.Dot(R, vUser) / sUser;

            if (tClosest > 0f)
            {
                // Aim for where the user will be at their closest point to the hazard spawn
                Vector3 closestPoint = pUser + (vUser * sUser * tClosest);
                return (closestPoint - pHazard).normalized;
            }
            else
            {
                // The user is already moving away from the closest point. Aim at their current position.
                return R.normalized;
            }
        }
    }
}
