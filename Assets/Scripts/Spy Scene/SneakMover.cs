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
    [SerializeField, Min(0f)] private float acceleration = 20f;
    [SerializeField, Min(0f)] private float deceleration = 24f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float turnSpeedDegPerSec = 720f;

    [Header("Input")]
    [SerializeField] private bool cameraRelative = true;
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift; // Shift toggles IsRunning

    [Header("Animator Params (optional)")]
    [SerializeField] private string movingBool = "IsMoving";
    [SerializeField] private string runningBool = "IsRunning";
    [SerializeField] private string speedFloat = "Speed";      // normalized 0..1

    [Header("Animator State (optional)")]
    [SerializeField] private string crouchWalkStateName = "";  // e.g., "CrouchWalk"
    [SerializeField] private int crouchLayerIndex = 0;
    [SerializeField] private bool ensureCrouchStateOnStart = true;

    [Header("Physics")]
    [SerializeField] private bool useGravity = false;
    [SerializeField, Min(0f)] private float stopDeadZone = 0.05f;

    // runtime
    private Vector3 currentVelocity;
    private Camera mainCam;
    private int crouchHash = -1;

    // animator param caching
    private int movingHash, runningHash, speedHash;
    private bool hasMoving, hasRunning, hasSpeed;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = useGravity;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
#else
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
#endif
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

        if (!string.IsNullOrEmpty(crouchWalkStateName))
            crouchHash = Animator.StringToHash(crouchWalkStateName);

        CacheAnimatorParams();
    }

    void Start()
    {
        if (animator && ensureCrouchStateOnStart && crouchHash != -1)
        {
            animator.Play(crouchHash, crouchLayerIndex, 0f);
            animator.Update(0f);
        }

        // Start paused; will unpause when movement input > 0
        if (animator) animator.speed = 0f;
    }

    void FixedUpdate()
    {
        // --- Input (WASD) ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        bool hasInput = input.sqrMagnitude > 0.0001f;
        bool sprintKey = Input.GetKey(runKey);

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
        float targetSpeed = (sprintKey && hasInput) ? runSpeed : walkSpeed;
        Vector3 targetVel = moveDir * (hasInput ? targetSpeed : 0f);

        // --- Accel / Decel (XZ) ---
        Vector3 planarCurrent = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Vector3 planarTarget = new Vector3(targetVel.x, 0f, targetVel.z);

        float rate = (planarTarget.sqrMagnitude > 0.0001f) ? acceleration : deceleration;
        Vector3 newPlanar = Vector3.MoveTowards(planarCurrent, planarTarget, rate * Time.fixedDeltaTime);
        currentVelocity = new Vector3(newPlanar.x, 0f, newPlanar.z);

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

        // --- Kill drift ---
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
        rb.angularVelocity = Vector3.zero;

        // --- Animator control (plays on ANY movement key) ---
        if (animator)
        {
            // Play animation when moving (W/A/S/D), pause when idle
            animator.speed = hasInput ? 1f : 0f;

            if (hasMoving) animator.SetBool(movingHash, hasInput);
            if (hasRunning) animator.SetBool(runningHash, hasInput && sprintKey);

            if (hasSpeed)
            {
                float norm = Mathf.InverseLerp(0f, runSpeed, currentVelocity.magnitude);
                animator.SetFloat(speedHash, norm, 0.1f, Time.fixedDeltaTime);
            }
        }
    }

    // --- Animator param caching ---
    void CacheAnimatorParams()
    {
        hasMoving = hasRunning = hasSpeed = false;
        if (!animator) return;

        movingHash = !string.IsNullOrEmpty(movingBool) ? Animator.StringToHash(movingBool) : 0;
        runningHash = !string.IsNullOrEmpty(runningBool) ? Animator.StringToHash(runningBool) : 0;
        speedHash = !string.IsNullOrEmpty(speedFloat) ? Animator.StringToHash(speedFloat) : 0;

        foreach (var p in animator.parameters)
        {
            if (p.nameHash == movingHash && p.type == AnimatorControllerParameterType.Bool) hasMoving = true;
            if (p.nameHash == runningHash && p.type == AnimatorControllerParameterType.Bool) hasRunning = true;
            if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float) hasSpeed = true;
        }

        if (!hasMoving && !string.IsNullOrEmpty(movingBool))
            Debug.LogWarning($"{name}: Animator bool '{movingBool}' not found.");
        if (!hasRunning && !string.IsNullOrEmpty(runningBool))
            Debug.LogWarning($"{name}: Animator bool '{runningBool}' not found.");
        if (!hasSpeed && !string.IsNullOrEmpty(speedFloat))
            Debug.LogWarning($"{name}: Animator float '{speedFloat}' not found.");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        CacheAnimatorParams();
    }
#endif
}
