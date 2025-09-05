using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaseAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;                     // Assign in inspector; falls back to tag "Player"
    public Animator animator;

    [Header("Animator")]
    public string runBoolParam = "IsRunning";

    [Header("Movement")]
    public float runSpeed = 4f;
    public float stoppingDistance = 1.5f;

    [Header("Detection")]
    public float viewRadius = 15f;
    [Range(0f, 360f)] public float viewAngle = 90f;
    public LayerMask obstacleLayer = ~0;         // Blocks line-of-sight
    public float eyeHeight = 1.6f;               // Ray origin height

    [Header("Diagnostics")]
    public bool alwaysChase = true;              // Force chase (ignore LOS) to verify NavMesh/agent
    public bool logDebug = true;                 // Minimal logs
    public bool drawGizmos = true;

    // ---- Internal ----
    NavMeshAgent agent;
    int runHash;
    bool hasRunParam;
    bool caughtPlayer;
    Vector3 lastKnownPos;
    bool playerInView;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        runHash = Animator.StringToHash(runBoolParam);
        CacheRunParam();
    }

    void Start()
    {
        // Acquire player if not assigned
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        EnsureOnNavMesh(true);

        // Agent setup
        if (HasValidAgentOnNavMesh)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.speed = runSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
        }

        if (logDebug)
        {
            if (!player) Debug.LogWarning($"{name}: No player assigned/found by tag.");
            if (!HasValidAgentOnNavMesh) Debug.LogWarning($"{name}: Agent not on NavMesh at start.");
        }
    }

    void Update()
    {
        if (caughtPlayer || !agent || !agent.enabled)
        {
            Idle(); return;
        }

        // If we somehow left the NavMesh (moving platforms etc.), try to recover
        if (!agent.isOnNavMesh) { EnsureOnNavMesh(false); if (!agent.isOnNavMesh) { Idle(); return; } }

        // If no player, just idle
        if (!player) { Idle(); return; }

        // Vision
        playerInView = HasLineOfSight();

        if (alwaysChase || playerInView)
        {
            lastKnownPos = player.position;
            Chase(lastKnownPos);
        }
        else if (lastKnownPos != Vector3.zero && Vector3.Distance(transform.position, lastKnownPos) > agent.stoppingDistance + 0.05f)
        {
            Chase(lastKnownPos); // go to last seen spot
        }
        else
        {
            Idle();
        }

        // Optional: simple catch if very close
        if (Vector3.Distance(transform.position, player.position) <= stoppingDistance + 0.25f)
        {
            // CaughtPlayer(); // call if you want to stop chasing on reach
        }
    }

    // -------- Behaviors --------
    void Chase(Vector3 dest)
    {
        SetRunBool(true);
        if (!HasValidAgentOnNavMesh) return;

        agent.isStopped = false;
        if (agent.speed != runSpeed) agent.speed = runSpeed;

        // Only update if changed to reduce path churn
        if ((agent.destination - dest).sqrMagnitude > 0.01f)
            agent.SetDestination(dest);

        // Optional debug
        if (logDebug && agent.pathStatus == NavMeshPathStatus.PathInvalid)
            Debug.LogWarning($"{name}: Invalid path to {dest}");
    }

    void Idle()
    {
        SetRunBool(false);
        if (!HasValidAgentOnNavMesh) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    public void CaughtPlayer()
    {
        caughtPlayer = true;
        Idle();
    }
    public void ReleasePlayer() { caughtPlayer = false; }

    // -------- Vision --------
    bool HasLineOfSight()
    {
        Vector3 to = player.position - transform.position;
        float dist = to.magnitude;
        if (dist > viewRadius) return false;

        Vector3 dir = to.normalized;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight * 0.9f;

        if (Physics.Raycast(origin, (target - origin).normalized, out RaycastHit hit, dist + 0.5f, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // If we hit something that isn't the player/child, blocked
            if (hit.transform != player && !hit.transform.IsChildOf(player)) return false;
        }
        // no blocking hit or we hit player
        return true;
    }

    // -------- Safety / Helpers --------
    bool HasValidAgentOnNavMesh => agent && agent.enabled && agent.isOnNavMesh;

    void EnsureOnNavMesh(bool verbose)
    {
        if (!agent || !agent.enabled) return;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, agent.areaMask))
            {
                agent.Warp(hit.position);
                if (verbose && logDebug) Debug.Log($"{name}: Warped to NavMesh at {hit.position}");
            }
            else
            {
                if (verbose && logDebug) Debug.LogWarning($"{name}: No NavMesh within 3m of {transform.position}");
            }
        }
    }

    void CacheRunParam()
    {
        hasRunParam = false;
        if (!animator) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == runHash)
            {
                hasRunParam = true; break;
            }
        }
        if (!hasRunParam && logDebug) Debug.LogWarning($"{name}: Animator bool '{runBoolParam}' not found.");
    }

    void SetRunBool(bool v)
    {
        if (animator && hasRunParam) animator.SetBool(runHash, v);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        runHash = Animator.StringToHash(runBoolParam);
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (agent) agent.stoppingDistance = stoppingDistance;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // vision cone
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + left * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + right * viewRadius);

        // destination line
        if (agent && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.destination);
        }

        // player line
        if (player)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * eyeHeight, player.position + Vector3.up * eyeHeight * 0.9f);
        }
    }
#endif
}
