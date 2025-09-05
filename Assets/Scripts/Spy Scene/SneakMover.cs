using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SneakMover : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    private Rigidbody rb;

    [Header("Movement Speeds")]
    [SerializeField, Min(0f)] private float walkSpeed = 2.5f;
    [SerializeField, Min(0f)] private float runSpeed = 5.0f;

    [Header("Acceleration")]
    [Tooltip("How fast you speed up towards target speed (units/sec^2).")]
    [SerializeField, Min(0f)] private float acceleration = 20f;
    [Tooltip("How fast you slow down towards zero (units/sec^2).")]
    [SerializeField, Min(0f)] private float deceleration = 24f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float turnSpeedDegPerSec = 720f;

    [Header("Input")]
    [SerializeField] private bool cameraRelative = true;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [Header("Animator Params (optional)")]
    [SerializeField] private string movingBool = "IsMoving";
    [SerializeField] private string runningBool = "IsRunning";
    [SerializeField] private string speedFloat = "Speed"; // normalized 0..1 (walk..run)

    [Header("Physics")]
    [SerializeField] private bool useGravity = false;
    [SerializeField, Min(0f)] private float stopDeadZone = 0.05f;

    // runtime
    private Vector3 currentVelocity; // XZ-plane velocity we control
    private Camera mainCam;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!GetComponent<CapsuleCollider>()) gameObject.AddComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        mainCam = Camera.main;
    }

    void FixedUpdate()
    {
        // --- Input (WASD) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(runKey);

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        // --- Camera-relative movement ---
        Vector3 moveDir = input;
        if (cameraRelative)
        {
            if (!mainCam) mainCam = Camera.main;
            if (mainCam)
            {
                Vector3 f = mainCam.transform.forward; f.y = 0f; f.Normalize();
                Vector3 r = mainCam.transform.right; r.y = 0f; r.Normalize();
                moveDir = f * v + r * h;
                if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
            }
        }

        // --- Target speed ---
        float targetSpeed = sprint ? runSpeed : walkSpeed;
        Vector3 targetVel = moveDir * targetSpeed;

        // --- Accel / Decel toward target velocity (XZ plane) ---
        Vector3 planarCurrent = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 planarTarget = new Vector3(targetVel.x, 0f, targetVel.z);

        // choose rate based on whether we are pressing input
        float rate = (planarTarget.sqrMagnitude > 0.0001f) ? acceleration : deceleration;
        Vector3 newPlanar = Vector3.MoveTowards(planarCurrent, planarTarget, rate * Time.fixedDeltaTime);
        currentVelocity = new Vector3(newPlanar.x, 0f, newPlanar.z); // keep on XZ

        // dead zone snap
        if (currentVelocity.magnitude < stopDeadZone) currentVelocity = Vector3.zero;

        // --- Move ---
        Vector3 delta = currentVelocity * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + delta);

        // --- Rotate to face move direction ---
        if (currentVelocity.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(currentVelocity.normalized, Vector3.up);
            Quaternion next = Quaternion.RotateTowards(rb.rotation, look, turnSpeedDegPerSec * Time.fixedDeltaTime);
            rb.MoveRotation(next);
        }

        // --- Kill drift in physics state (since we use MovePosition/Rotation) ---
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // --- Animator sync ---
        bool isMoving = currentVelocity.sqrMagnitude > 0.0001f;
        if (!string.IsNullOrEmpty(movingBool)) animator?.SetBool(movingBool, isMoving);
        if (!string.IsNullOrEmpty(runningBool)) animator?.SetBool(runningBool, isMoving && sprint);
        if (!string.IsNullOrEmpty(speedFloat))
        {
            float norm = Mathf.InverseLerp(0f, runSpeed, currentVelocity.magnitude);
            animator?.SetFloat(speedFloat, norm, 0.1f, Time.fixedDeltaTime); // damp for smooth blend trees
        }
    }
}
