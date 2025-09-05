using UnityEngine;
using UnityEngine.UI;

public class SpyingProgressBar : MonoBehaviour
{
    [Header("References (assign Slider in Inspector; Listening is set by spawner)")]
    [SerializeField] private Listening listening;   // set at runtime by spawner
    [SerializeField] private Slider slider;         // the exact UI Slider to drive

    public void SetListening(Listening l) => listening = l;

    void Awake()
    {
        if (!slider)
        {
            Debug.LogError("[SpyingProgressBar] Slider not assigned.");
            enabled = false;
            return;
        }

        // Ensure correct slider setup
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.interactable = false;

        // Must have Fill Rect wired or it won't render filling
        if (slider.fillRect == null)
            Debug.LogError("[SpyingProgressBar] Slider.Fill Rect is NOT set. Drag 'Fill Area/Fill' into Fill Rect on the Slider.");
    }

    void Update()
    {
        if (!listening || !slider) return;
        slider.SetValueWithoutNotify(Mathf.Clamp01(listening.progress));
    }
}
