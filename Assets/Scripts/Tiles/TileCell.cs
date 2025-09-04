using UnityEngine;

[DisallowMultipleComponent]
public class TileCell : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Kingdom owner = Kingdom.None;
    [SerializeField] private Resources values; // Gold/Wood/Influence from type.Values (Population excluded)
    [SerializeField] private TileType type;

    [Header("Population (runtime per-tile)")]
    [SerializeField] private int currentPopulation = 0;

    [Header("Grid Coords (set by HexPlacer)")]
    [SerializeField] private int x = -1;
    [SerializeField] private int z = -1;

    public Kingdom Owner => owner;
    public Resources Values => values;
    public TileType Type => type;
    public int X => x;
    public int Z => z;
    public bool HasCoords => x >= 0 && z >= 0;
    public int CurrentPopulation => currentPopulation;

    void OnEnable()
    {
        if (HasCoords) TileRegistry.Register(x, z, this);
    }

    void OnDisable()
    {
        if (HasCoords) TileRegistry.Unregister(x, z, this);
    }

    public void SetCoords(int cx, int cz, HexGrid grid)
    {
        x = cx; z = cz;
        if (grid) TileRegistry.SetGrid(grid);
        if (isActiveAndEnabled) TileRegistry.Register(x, z, this);
    }

    public void InitFromType(TileType t)
    {
        type = t;

        // Copy only non-Population yields into Values; Population is handled by the fields below.
        values = t
            ? new Resources { Gold = t.Values.Gold, Wood = t.Values.Wood, Influence = t.Values.Influence, Population = 0 }
            : Resources.Zero;

        // Initialize per-tile population runtime
        currentPopulation = (t != null)
            ? Mathf.Clamp(t.InitialPopulation, 0, Mathf.Max(0, t.MaxPopulation))
            : 0;
    }

    // Placement pays cost and grants InitialPopulation to the owning kingdom once.
    public bool TryPlace(Kingdom placingKingdom)
    {
        if (!type) { Debug.LogError("TileCell.TryPlace: TileType missing."); return false; }
        var gm = GameManager.Instance; if (!gm) return false;

        if (placingKingdom == Kingdom.None)
        {
            owner = Kingdom.None;
            return true;
        }

        if (!gm.TrySpend(placingKingdom, type.Cost)) return false;

        owner = placingKingdom;

        // Award initial population to the owning kingdom's global total
        if (type.MaxPopulation > 0 && currentPopulation > 0)
        {
            gm.AddTo(owner, new Resources { Population = currentPopulation });
        }

        return true;
    }

    public void ForceSetOwner(Kingdom k) { owner = k; }
    public void SetValues(Resources r) { values = r; }

    /// <summary>
    /// Advance per-tile population by one turn, return the delta added this turn.
    /// </summary>
    public int GrowPopulationOneTurn()
    {
        if (type == null || owner == Kingdom.None) return 0;
        if (type.MaxPopulation <= 0 || type.PopulationPerTurn <= 0) return 0;

        int remaining = type.MaxPopulation - currentPopulation;
        if (remaining <= 0) return 0;

        int delta = Mathf.Min(type.PopulationPerTurn, remaining);
        currentPopulation += delta;
        return delta;
    }
}
