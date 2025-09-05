using TMPro;
using UnityEngine;

public class PlayerResourcesUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text influenceText;

    private GameManager gm;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (!gm) return;

        gm.OnPlayerGoldChanged.AddListener(UpdateGold);
        gm.OnPlayerWoodChanged.AddListener(UpdateWood);
        gm.OnPlayerInfluenceChanged.AddListener(UpdateInfluence);

        // Force initial sync
        var w = gm.GetWallet(Kingdom.Player);
        UpdateGold(w.Gold);
        UpdateWood(w.Wood);
        UpdateInfluence(w.Influence);
    }

    void OnDisable()
    {
        if (!gm) return;
        gm.OnPlayerGoldChanged.RemoveListener(UpdateGold);
        gm.OnPlayerWoodChanged.RemoveListener(UpdateWood);
        gm.OnPlayerInfluenceChanged.RemoveListener(UpdateInfluence);
    }

    void UpdateGold(int v) { if (goldText) goldText.text = $"+{v}"; }
    void UpdateWood(int v) { if (woodText) woodText.text = $"+{v}"; }
    void UpdateInfluence(int v) { if (influenceText) influenceText.text = $"+{v}"; }
}
