using UnityEngine;

[DefaultExecutionOrder(50)]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                     // set by spawner (Scene 2 only)
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Orbit")]
    public float yawSensitivity = 180f;
    public float pitchSensitivity = 180f;
    public bool invertY = false;
    [Range(-89f, 89f)] public float minPitch = -35f;
    [Range(-89f, 89f)] public float maxPitch = 70f;

    [Header("Zoom")]
    public float distance = 4f;
    public float minDistance = 1.0f;
    public float maxDistance = 8.0f;
    public float zoomSensitivity = 5f;

    [Header("Smoothing")]
    [Range(0f, 0.25f)] public float positionSmooth = 0.05f;
    [Range(0f, 0.25f)] public float rotationSmooth = 0.05f;

    [Header("Collision")]
    public LayerMask obstacleLayers = ~0;        // exclude Player layer if needed
    public float collideRadius = 0.2f;
    public float collideBuffer = 0.05f;

    float yaw, pitch;
    float yawVel, pitchVel;
    Vector3 posVel;

    void Start()
    {
        if (target)
        {
            Vector3 fwd = target.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                yaw = Quaternion.LookRotation(fwd).eulerAngles.y;
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (!target) return;

        // Read mouse deltas (NO cursor locking here)
        float mx = Input.GetAxisRaw("Mouse X");
        float my = Input.GetAxisRaw("Mouse Y");
        float ySign = invertY ? 1f : -1f;

        float targetYaw = yaw + mx * yawSensitivity * Time.deltaTime;
        float targetPitch = Mathf.Clamp(pitch + ySign * my * pitchSensitivity * Time.deltaTime, minPitch, maxPitch);

        yaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, rotationSmooth);
        pitch = Mathf.SmoothDampAngle(pitch, targetPitch, ref pitchVel, rotationSmooth);

        // Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);

        // Desired camera pose
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 desiredPos = pivot - rot * Vector3.forward * distance;

        // Collision
        Vector3 dir = desiredPos - pivot;
        float maxCheck = Mathf.Max(0f, dir.magnitude);
        if (maxCheck > 0.001f)
        {
            dir /= maxCheck;
            if (Physics.SphereCast(pivot, collideRadius, dir, out RaycastHit hit, maxCheck, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                float camDist = Mathf.Max(minDistance, hit.distance - collideBuffer);
                desiredPos = pivot + dir * camDist;
            }
        }

        // Apply
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmooth);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSmooth > 0f ? Time.deltaTime / rotationSmooth : 1f);
        transform.LookAt(pivot);
    }
}
