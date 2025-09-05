using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WireNextTurn : MonoBehaviour
{
    [SerializeField] private Button nextTurnBtn; // optional

    void OnEnable()
    {
        if (!nextTurnBtn) nextTurnBtn = GetComponent<Button>();
        if (!nextTurnBtn) { Debug.LogError("WireNextTurn: No Button found."); return; }

        nextTurnBtn.onClick.RemoveAllListeners();

        // Find the live TurnSystem in the current scene (active or inactive)
#if UNITY_2023_1_OR_NEWER
        var ts = Object.FindFirstObjectByType<TurnSystem>(FindObjectsInactive.Include);
#else
        var ts = FindObjectOfType<TurnSystem>(true);
#endif
        if (ts) nextTurnBtn.onClick.AddListener(ts.NextTurn);
        else Debug.LogError("WireNextTurn: No TurnSystem found.");
    }
}
