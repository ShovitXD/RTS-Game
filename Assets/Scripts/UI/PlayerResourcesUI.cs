using TMPro;
using UnityEngine;

public class PlayerResourcesUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager; // optional; auto-grabs if left empty
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text influenceText;

    void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance;

        if (gameManager != null)
        {
            gameManager.OnPlayerGoldChanged.AddListener(UpdateGold);
            gameManager.OnPlayerWoodChanged.AddListener(UpdateWood);
            gameManager.OnPlayerInfluenceChanged.AddListener(UpdateInfluence);
        }
    }

    void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnPlayerGoldChanged.RemoveListener(UpdateGold);
            gameManager.OnPlayerWoodChanged.RemoveListener(UpdateWood);
            gameManager.OnPlayerInfluenceChanged.RemoveListener(UpdateInfluence);
        }
    }

    void Start()
    {
        // Force initial sync
        if (gameManager != null)
        {
            var wallet = gameManager.GetWallet(Kingdom.Player);
            UpdateGold(wallet.Gold);
            UpdateWood(wallet.Wood);
            UpdateInfluence(wallet.Influence);
        }
    }

    void UpdateGold(int value) => SetText(goldText, $"+{value}");
    void UpdateWood(int value) => SetText(woodText, $"+{value}");
    void UpdateInfluence(int value) => SetText(influenceText, $"+{value}");

    void SetText(TMP_Text txt, string msg)
    {
        if (txt) txt.text = msg;
    }
}
