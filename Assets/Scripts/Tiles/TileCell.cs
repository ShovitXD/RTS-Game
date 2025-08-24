using UnityEngine;

[DisallowMultipleComponent]
public class TileCell : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Kingdom owner = Kingdom.None;  // 0..4 owned, 5 = Unowned
    [SerializeField] private Resources values;              // per-cell payload
    [SerializeField] private TileType type;                 // SO blueprint

    [Header("Grid Coords (set by HexPlacer)")]
    [SerializeField] private int x = -1;  // col
    [SerializeField] private int z = -1;  // row

    public Kingdom Owner => owner;
    public Resources Values => values;
    public TileType Type => type;
    public int X => x;
    public int Z => z;
    public bool HasCoords => x >= 0 && z >= 0;

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
        // If already enabled, ensure registry has us at the right coords
        if (isActiveAndEnabled) TileRegistry.Register(x, z, this);
    }

    public void InitFromType(TileType t)
    {
        type = t;
        values = t ? t.Values : Resources.Zero;
    }

    // Placement pays cost unless placing as Unowned
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
        return true;
    }

    public void ForceSetOwner(Kingdom k) { owner = k; }
    public void SetValues(Resources r) { values = r; }
}
