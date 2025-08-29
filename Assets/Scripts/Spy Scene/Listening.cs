using UnityEngine;

public class Listening : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object to listen to (place empty object in scene).")]
    public Transform listeningTarget;

    [Header("Settings")]
    [Tooltip("How close the player must be to start listening.")]
    public float listeningRange = 5f;
    [Tooltip("How long (seconds) it takes to fill the listening meter.")]
    public float fillDuration = 3f;

    [Header("Status")]
    [Range(0f, 1f)] public float progress = 0f; // 0 = empty, 1 = full
    public bool hasSpied = false;               // set true when fully listened

    private bool isInRange => listeningTarget &&
                              Vector3.Distance(transform.position, listeningTarget.position) <= listeningRange;

    void Update()
    {
        if (hasSpied || listeningTarget == null) return;

        if (isInRange)
        {
            // Fill progress over time
            progress += Time.deltaTime / fillDuration;
            if (progress >= 1f)
            {
                progress = 1f;
                hasSpied = true;
                Debug.Log("Listening complete! hasSpied = true");
            }
        }
        else
        {
            // Optional: reset when out of range
            if (progress > 0f)
            {
                progress -= Time.deltaTime / fillDuration;
                if (progress < 0f) progress = 0f;
            }
        }
    }
}
