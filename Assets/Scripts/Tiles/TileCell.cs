using UnityEngine;

[DisallowMultipleComponent]
public class TileCell : MonoBehaviour
{
    [SerializeField] private Kingdom owner = Kingdom.None;
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

    public bool TryPlace(Kingdom placingKingdom)
    {
        if (!type) { Debug.LogError("TileCell.TryPlace: TileType missing."); return false; }
        var gm = GameManager.Instance; if (!gm) return false;
        if (!gm.TrySpend(placingKingdom, type.Cost)) return false;
        owner = placingKingdom;
        return true;
    }

    public void ForceSetOwner(Kingdom k) { owner = k; }
    public void SetValues(Resources r) { values = r; }
}
