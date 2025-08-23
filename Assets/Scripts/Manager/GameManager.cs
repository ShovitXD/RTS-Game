using UnityEngine;
using UnityEngine.Events;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // We keep 5 wallets (0..4). 'None' is 5 and has NO wallet.
    public const int KingdomCount = 5;

    [Header("Dev")]
    [SerializeField] private bool devMode = false;
    public bool DevMode => devMode;

    [Header("Wallets (Player, Enemy, Friendly, Faction3, Faction4)")]
    [SerializeField] private Resources[] wallets = new Resources[KingdomCount];

    // Optional: player UI hooks
    public UnityEvent<int> OnPlayerGoldChanged;
    public UnityEvent<int> OnPlayerWoodChanged;
    public UnityEvent<int> OnPlayerInfluenceChanged;

    [Header("Turn System (stub for future)")]
    [SerializeField] private int turnNumber = 0;
    public UnityEvent<int> OnTurnChanged;

    [Header("AI")]
    [SerializeField] private Kingdom expandingAI = Kingdom.Enemy; // Enemy is the expanding AI
    public Kingdom Player => Kingdom.Player;
    public Kingdom ExpandingAI => expandingAI;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (expandingAI == Kingdom.Player) expandingAI = Kingdom.Enemy;
        ClampWalletArray();
    }

    void ClampWalletArray()
    {
        if (wallets == null || wallets.Length != KingdomCount)
            wallets = new Resources[KingdomCount];
    }

    // --- Turn flow (expand later) ---
    public int CurrentTurn => turnNumber;

    public void BeginTurn()
    {
        // hook per-turn start logic here (effects, upkeep, etc.)
    }

    public void EndTurn()
    {
        turnNumber++;
        OnTurnChanged?.Invoke(turnNumber);
        // hook per-turn end logic here (process queued actions, yields, etc.)
    }

    // --- Wallet helpers ---
    static bool IsOwnedKingdom(Kingdom k) => k != Kingdom.None;
    static bool TryIndex(Kingdom k, out int idx)
    {
        idx = (int)k;
        return IsOwnedKingdom(k) && idx >= 0 && idx < KingdomCount;
    }

    public Resources GetWallet(Kingdom k)
    {
        if (TryIndex(k, out int i)) return wallets[i];
        return Resources.Zero; // None or out of range -> zero
    }

    public void SetWallet(Kingdom k, Resources r)
    {
        if (!TryIndex(k, out int i)) return; // ignore None
        wallets[i] = r;
        if (k == Kingdom.Player) RaisePlayerEvents(r);
    }

    public void AddTo(Kingdom k, Resources delta)
    {
        if (!TryIndex(k, out int i)) return; // ignore None
        var r = wallets[i] + delta;
        r.ClampNonNegative();
        wallets[i] = r;
        if (k == Kingdom.Player) RaisePlayerEvents(r);
    }

    public bool TrySpend(Kingdom k, Resources cost)
    {
        if (k == Kingdom.None) return true; // unowned pays nothing
        if (DevMode) cost = Resources.Zero;

        if (!TryIndex(k, out int i)) return false;

        var r = wallets[i];
        if (!r.CanAfford(cost)) return false;

        r -= cost;
        r.ClampNonNegative();
        wallets[i] = r;
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
