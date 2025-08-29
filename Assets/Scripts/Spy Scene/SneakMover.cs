using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class SneakMover : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    private Rigidbody rb;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float sneakSpeed = 2.0f;   // speed in SneakWalk
    [SerializeField, Min(0f)] private float runSpeed = 3.5f;   // speed in Run
    [SerializeField] private bool cameraRelative = true;         // WASD relative to camera

    [Header("Run Input")]
    [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float turnSpeed = 720f;    // deg/sec to face move dir

    [Header("Animator Params")]
    [SerializeField] private string movingBool = "IsMoving";    // true while moving
    [SerializeField] private string runningBool = "IsRunning";   // true while moving + Shift

    [Header("Physics Tweaks")]
    [SerializeField] private bool useGravity = false;            // off for top-down scenes
    [SerializeField, Min(0f)] private float stopDeadZone = 0.01f;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = useGravity;
        rb.linearDamping = 0f; // no artificial drag, we control stopping ourselves
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
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        // Input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool runHeld = Input.GetKey(runKey);

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (cameraRelative && Camera.main)
        {
            Vector3 f = Camera.main.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = Camera.main.transform.right; r.y = 0f; r.Normalize();
            input = f * v + r * h;
            if (input.sqrMagnitude > 1f) input.Normalize();
        }

        bool isMoving = input.sqrMagnitude > 0.0001f;
        float moveSpeed = (isMoving && runHeld) ? runSpeed : sneakSpeed;

        // Calculate velocity directly — no acceleration smoothing
        Vector3 velocity = isMoving ? input * moveSpeed : Vector3.zero;

        // Move instantly
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // Face movement direction
        if (isMoving)
        {
            Quaternion look = Quaternion.LookRotation(input, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, look, turnSpeed * Time.fixedDeltaTime));
        }

        // Stop drift when idle
        if (!isMoving)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Animator sync
        if (!string.IsNullOrEmpty(movingBool)) animator?.SetBool(movingBool, isMoving);
        if (!string.IsNullOrEmpty(runningBool)) animator?.SetBool(runningBool, isMoving && runHeld);

        // Pause playback when idle
        if (animator) animator.speed = isMoving ? 1f : 0f;
    }
}
