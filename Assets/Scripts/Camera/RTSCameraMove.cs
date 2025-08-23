// RTSCameraMove.cs
using UnityEngine;

[DisallowMultipleComponent]
public class RTSCameraMove : MonoBehaviour
{
    [Header("Movement (XZ)")]
    [SerializeField] float moveSpeed = 10f;          // units/sec
    [SerializeField] float boostMultiplier = 2f;     // hold Left Shift to boost
    [SerializeField] bool cameraRelative = true;     // WASD follows camera yaw on XZ

    [Header("Zoom (Height only)")]
    [SerializeField] float zoomSpeed = 10f;          // scroll sensitivity
    [SerializeField] float minHeight = 5f;
    [SerializeField] float maxHeight = 60f;
    [SerializeField] bool smoothZoom = true;
    [SerializeField] float zoomSmoothing = 10f;      // higher = snappier

    [Header("Bounds (optional)")]
    [SerializeField] bool clampToArea = false;
    [SerializeField] Vector2 minXZ = new Vector2(-50f, -50f);
    [SerializeField] Vector2 maxXZ = new Vector2(50f, 50f);

    float targetHeight;

    void Start()
    {
        targetHeight = transform.position.y;
    }

    void Update()
    {
        HandleMove();
        HandleZoom();
        ClampIfNeeded();
    }

    void HandleMove()
    {
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;

        Vector3 dir;
        if (cameraRelative)
        {
            Vector3 fwd = ForwardOnPlane();
            Vector3 right = RightOnPlane();
            dir = right * x + fwd * z;
        }
        else
        {
            dir = new Vector3(x, 0f, z);
        }

        if (dir.sqrMagnitude > 1e-4f) dir.Normalize();

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f);
        transform.position += dir * speed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y; // positive = zoom in
        if (Mathf.Abs(scroll) > 1e-4f)
            targetHeight = Mathf.Clamp(targetHeight - scroll * zoomSpeed, minHeight, maxHeight);

        float currentY = transform.position.y;
        float nextY = smoothZoom
            ? Mathf.Lerp(currentY, targetHeight, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime))
            : targetHeight;

        Vector3 p = transform.position;
        p.y = nextY;
        transform.position = p;
    }

    void ClampIfNeeded()
    {
        if (!clampToArea) return;
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minXZ.x, maxXZ.x);
        p.z = Mathf.Clamp(p.z, minXZ.y, maxXZ.y);
        transform.position = p;
    }

    Vector3 ForwardOnPlane()
    {
        Vector3 f = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }

    Vector3 RightOnPlane()
    {
        Vector3 r = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        return r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right;
    }
}
