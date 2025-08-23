using UnityEngine;
using UnityEngine.Events;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public const int KingdomCount = 5;

    [Header("Dev")]
    [SerializeField] private bool devMode = false;
    public bool DevMode => devMode;

    [Header("Wallets (Player + 4 AI)")]
    [SerializeField] private Resources[] wallets = new Resources[KingdomCount];

    public UnityEvent<int> OnPlayerGoldChanged;
    public UnityEvent<int> OnPlayerWoodChanged;
    public UnityEvent<int> OnPlayerInfluenceChanged;

    [Header("AI")]
    [SerializeField] private Kingdom expandingAI = Kingdom.AI1;
    public Kingdom Player => Kingdom.Player;
    public Kingdom ExpandingAI => expandingAI;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (expandingAI == Kingdom.Player) expandingAI = Kingdom.AI1;
    }

    public Resources GetWallet(Kingdom k) => wallets[(int)k];

    public void SetWallet(Kingdom k, Resources r)
    {
        wallets[(int)k] = r;
        if (k == Kingdom.Player) RaisePlayerEvents(r);
    }

    public void AddTo(Kingdom k, Resources delta)
    {
        var r = wallets[(int)k] + delta;
        r.ClampNonNegative();
        wallets[(int)k] = r;
        if (k == Kingdom.Player) RaisePlayerEvents(r);
    }

    public bool TrySpend(Kingdom k, Resources cost)
    {
        if (DevMode) cost = Resources.Zero;
        var r = wallets[(int)k];
        if (!r.CanAfford(cost)) return false;
        r -= cost;
        r.ClampNonNegative();
        wallets[(int)k] = r;
        if (k == Kingdom.Player) RaisePlayerEvents(r);
        return true;
    }

    void RaisePlayerEvents(Resources r)
    {
        OnPlayerGoldChanged?.Invoke(r.Gold);
        OnPlayerWoodChanged?.Invoke(r.Wood);
        OnPlayerInfluenceChanged?.Invoke(r.Influence);
    }
}
