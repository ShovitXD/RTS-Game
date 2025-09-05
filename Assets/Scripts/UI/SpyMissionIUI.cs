using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpyMissionUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private Button goButton;

    [Header("Config")]
    [SerializeField] private int showAfterTurn = 3;
    [SerializeField] private int sceneIndexToLoad = 2;
    [SerializeField, TextArea]
    private string message =
        "Your spy has infiltrated the nearest kingdom. Go to the spy mission";

    private GameManager gm;
    private bool shownOnce;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (panel) panel.SetActive(false);

        if (goButton) { goButton.onClick.RemoveAllListeners(); goButton.onClick.AddListener(OnGoPressed); }
        if (gm) gm.OnTurnChanged.AddListener(OnTurnChanged);

        if (gm && gm.CurrentTurn >= showAfterTurn && !shownOnce) ShowNow();
    }

    void OnDisable()
    {
        if (gm) gm.OnTurnChanged.RemoveListener(OnTurnChanged);
        if (goButton) goButton.onClick.RemoveListener(OnGoPressed);
    }

    void OnTurnChanged(int newTurn) { if (!shownOnce && newTurn >= showAfterTurn) ShowNow(); }

    void ShowNow()
    {
        if (notificationText) notificationText.text = message;
        if (panel) panel.SetActive(true);
        shownOnce = true;
    }

    void OnGoPressed()
    {
        if (panel) panel.SetActive(false);
        SceneManager.LoadScene(sceneIndexToLoad);
    }
}
