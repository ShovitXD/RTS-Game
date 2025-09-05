using UnityEngine;
using UnityEngine.SceneManagement;

public class SpyMissionSpawner : MonoBehaviour
{
    [Header("Spawn & Exit")]
    [SerializeField] private GameObject playerPrefab;   // player prefab (with Listening)
    [SerializeField] private Transform spawnExitPoint;  // one empty used for spawn + exit

    [Header("Return")]
    [SerializeField] private int returnSceneIndex = 1;

    [Header("Exit Detection")]
    [SerializeField] private bool useTriggerExit = true; // true = trigger; false = distance check
    [SerializeField, Min(0f)] private float exitRadius = 2f;

    [Header("Camera (Scene 2 only)")]
    [SerializeField] private bool setCameraTarget = true; // assign Scene 2 ThirdPersonCamera target

    [Header("UI (optional)")]
    [SerializeField] private SpyingProgressBar progressBar; // UI bar script

    private GameObject playerInstance;
    private Listening listening;

    void Start()
    {
        if (!playerPrefab || !spawnExitPoint)
        {
            Debug.LogError("SpyMissionSpawner: Assign playerPrefab and spawnExitPoint.");
            enabled = false;
            return;
        }

        // Spawn player
        playerInstance = Instantiate(playerPrefab, spawnExitPoint.position, spawnExitPoint.rotation);
        listening = playerInstance.GetComponent<Listening>();
        if (!listening) Debug.LogWarning("SpyMissionSpawner: Player prefab has no Listening component.");

        // Bind UI progress bar
#if UNITY_2023_1_OR_NEWER
        if (!progressBar) progressBar = Object.FindFirstObjectByType<SpyingProgressBar>(FindObjectsInactive.Include);
#else
        if (!progressBar) progressBar = FindObjectOfType<SpyingProgressBar>(true);
#endif
        if (progressBar) progressBar.SetListening(listening);

        // Scene 2 camera follow ONLY (no cursor logic)
        if (setCameraTarget && Camera.main)
        {
            var camController = Camera.main.GetComponent<ThirdPersonCamera>(); // Scene 2 script
            if (camController) camController.target = playerInstance.transform;
        }

        // Exit trigger auto-setup
        if (useTriggerExit)
        {
            var col = spawnExitPoint.GetComponent<SphereCollider>();
            if (!col) col = spawnExitPoint.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = exitRadius;

            var proxy = spawnExitPoint.GetComponent<ExitTriggerProxy>();
            if (!proxy) proxy = spawnExitPoint.gameObject.AddComponent<ExitTriggerProxy>();
            proxy.Init(this);
        }
    }

    void Update()
    {
        if (!playerInstance || !spawnExitPoint || useTriggerExit) return;
        if (!listening || !listening.hasSpied) return;

        // Distance-based fallback
        if (Vector3.Distance(playerInstance.transform.position, spawnExitPoint.position) <= exitRadius)
            ReturnToBaseScene();
    }

    // Called by ExitTriggerProxy on trigger enter
    internal void HandleExitTrigger(Collider other)
    {
        if (!useTriggerExit || !playerInstance || !listening) return;
        if (!listening.hasSpied) return;

        // Ensure it's our player's rigidbody
        if (other.attachedRigidbody && other.attachedRigidbody.gameObject == playerInstance)
            ReturnToBaseScene();
    }

    private void ReturnToBaseScene()
    {
        // No cursor or Scene 1 camera edits.
        SceneManager.LoadScene(returnSceneIndex);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (spawnExitPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnExitPoint.position, exitRadius);
        }
    }
#endif
}

[DisallowMultipleComponent]
public class ExitTriggerProxy : MonoBehaviour
{
    private SpyMissionSpawner owner;
    public void Init(SpyMissionSpawner spawner) => owner = spawner;

    void OnTriggerEnter(Collider other)
    {
        if (owner != null) owner.HandleExitTrigger(other);
    }
}
