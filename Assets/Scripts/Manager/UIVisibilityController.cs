using UnityEngine;

public class UIVisibilityController : MonoBehaviour
{
    public GameObject[] devUIElements;
    private GameManager gm;

    void OnEnable()
    {
        gm = GameManager.Instance;
        ApplyVisibility();
    }

    void Update() => ApplyVisibility();

    void ApplyVisibility()
    {
        if (devUIElements == null || gm == null) return;
        bool on = gm.DevMode;
        foreach (var ui in devUIElements)
            if (ui && ui.activeSelf != on) ui.SetActive(on);
    }
}
