using UnityEngine;

[DisallowMultipleComponent]
public class TileCell : MonoBehaviour
{
    [SerializeField] private Kingdom owner = Kingdom.None; // 5 by enum
    [SerializeField] private Resources values;
    [SerializeField] private TileType type;

    public Kingdom Owner => owner;
    public Resources Values => values;
    public TileType Type => type;

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
