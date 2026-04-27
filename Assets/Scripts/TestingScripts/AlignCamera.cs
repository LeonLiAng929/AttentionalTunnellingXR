using UnityEngine;

public class AlignCameraRig : MonoBehaviour
{
    [SerializeField] OVRCameraRig _cameraRig;

    OVRLocatable _locatable;

    async void Start()
    {
        // Create and localize the anchor
        var anchor = await OVRAnchor.CreateSpatialAnchorAsync(Pose.identity);
        if (anchor == OVRAnchor.Null)
        {
            Debug.LogError("Unable to create spatial anchor.");
            return;
        }

        var locatable = anchor.GetComponent<OVRLocatable>();
        if (!await locatable.SetEnabledAsync(true))
        {
            Debug.LogError("Unable to localize spatial anchor.");
            return;
        }

        _locatable = locatable;
    }

    void Update()
    {
        if (!_locatable.IsNull &&
            _locatable.TryGetSpatialAnchorPose(out var pose) &&
            pose.Position.HasValue &&
            pose.Rotation.HasValue)
        {
            // Apply the inverse pose to the camera rig.
            transform.SetPositionAndRotation(pose.Position.Value, pose.Rotation.Value);
            _cameraRig.transform.position = transform.InverseTransformPoint(Vector3.zero);
            _cameraRig.transform.eulerAngles = new Vector3(0, -transform.eulerAngles.y, 0);
        }
    }
}