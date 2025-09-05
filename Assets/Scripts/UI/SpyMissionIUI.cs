using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpyMissionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;   // optional; auto-grabs
    [SerializeField] private GameObject panel;          // the whole panel (starts inactive)
    [SerializeField] private TMP_Text notificationText; // child of panel
    [SerializeField] private Button goButton;           // child of panel

    [Header("Config")]
    [Tooltip("Show the notification after this turn number is reported by GameManager.OnTurnChanged.")]
    [SerializeField] private int showAfterTurn = 3;     // "when 3rd turn is over"
    [SerializeField] private int sceneIndexToLoad = 2;  // load this scene when button is pressed
    [SerializeField, TextArea]
    private string message =
        "Your spy has infiltrated the nearest kingdom. Go to the spy mission";

    private bool shownOnce = false;

    void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance;

        // Ensure hidden at start
        if (panel) panel.SetActive(false);

        if (goButton) goButton.onClick.AddListener(OnGoPressed);
    }

    void OnEnable()
    {
        if (gameManager) gameManager.OnTurnChanged.AddListener(OnTurnChanged);
    }

    void OnDisable()
    {
        if (gameManager) gameManager.OnTurnChanged.RemoveListener(OnTurnChanged);
        if (goButton) goButton.onClick.RemoveListener(OnGoPressed);
    }

    void Start()
    {
        // If already past the target turn when entering the scene, show immediately
        if (gameManager && gameManager.CurrentTurn >= showAfterTurn && !shownOnce)
            ShowNow();
    }

    private void OnTurnChanged(int newTurn)
    {
        // EndTurn() increments then fires this event; at newTurn == 3, "3rd turn is over"
        if (!shownOnce && newTurn >= showAfterTurn)
            ShowNow();
    }

    private void ShowNow()
    {
        if (notificationText) notificationText.text = message;
        if (panel) panel.SetActive(true);
        shownOnce = true; // only once
    }

    private void OnGoPressed()
    {
        if (panel) panel.SetActive(false); // hide before leaving
        SceneManager.LoadScene(sceneIndexToLoad);
    }
}
