using UnityEngine;

public class UIVisibilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager; // drag GameManager or leave blank to auto-grab

    [Header("UI Elements to Toggle")]
    public GameObject[] devUIElements;

    void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance;
    }

    void Start()
    {
        ApplyVisibility();
    }

    void Update()
    {
        // Refresh each frame in case DevMode can change at runtime
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (devUIElements == null || gameManager == null) return;

        bool on = gameManager.DevMode;
        foreach (var ui in devUIElements)
        {
            if (ui != null && ui.activeSelf != on)
                ui.SetActive(on);
        }
    }
}
