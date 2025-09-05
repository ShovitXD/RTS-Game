using UnityEngine;

public class UIVisibilityController : MonoBehaviour
{
    [Header("Toggle Dev Mode")]
    public bool devMode = true;

    [Header("UI Elements to Toggle")]
    public GameObject[] devUIElements;

    void Start()
    {
        ApplyVisibility();
    }

    void OnValidate() // updates instantly in inspector
    {
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        if (devUIElements == null) return;

        foreach (var ui in devUIElements)
        {
            if (ui != null)
                ui.SetActive(devMode);
        }
    }
}
