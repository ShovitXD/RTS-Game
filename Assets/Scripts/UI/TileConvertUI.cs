using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileConvertUI : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private HexPlacer hexPlacer;        // scene HexPlacer
    [SerializeField] private TMP_Dropdown tileDropdown;  // populated by TileDropdownBinder
    [SerializeField] private Kingdom playerKingdom = Kingdom.Player;

    [Header("Validation")]
    [SerializeField] private bool warnIfTemplateMissing = true;

    private GameManager gm;

    void OnEnable()
    {
        gm = GameManager.Instance;
        if (!hexPlacer) hexPlacer = FindObjectOfType<HexPlacer>(true);

        if (tileDropdown)
        {
            tileDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            tileDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    void OnDisable()
    {
        if (tileDropdown) tileDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    // === Button hook ===
    public void ShowDropdown()
    {
        if (!tileDropdown) { Debug.LogWarning("TileConvertUI: tileDropdown is not assigned."); return; }
        if (!EnsureEventSystem()) return;
        if (!EnsureDropdownTemplate(tileDropdown)) return;
        if (!EnsureHasOptions(tileDropdown)) return;

        if (!tileDropdown.gameObject.activeSelf)
            tileDropdown.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tileDropdown.GetComponent<RectTransform>());

        tileDropdown.enabled = true;
        tileDropdown.interactable = true;

        StartCoroutine(ShowNextFrame());
    }

    IEnumerator ShowNextFrame() { yield return null; if (tileDropdown) tileDropdown.Show(); }

    public void HideDropdown()
    {
        if (!tileDropdown) return;
        tileDropdown.Hide();
        tileDropdown.gameObject.SetActive(false);
    }

    // Optional legacy confirm
    public void OnClickConvert() => TryConvertToIndex(tileDropdown ? tileDropdown.value : 0);

    void OnDropdownChanged(int newIndex)
    {
        TryConvertToIndex(newIndex);
        HideDropdown();
    }

    void TryConvertToIndex(int idx)
    {
        var targetCell = HexPlacer.LastRightClickedCell;
        if (!targetCell || !targetCell.HasCoords) { Debug.Log("Convert: no target cell."); return; }
        if (targetCell.Owner != playerKingdom) { Debug.Log("Convert: not your tile."); return; }

        if (!hexPlacer) { Debug.LogError("Convert: HexPlacer reference not assigned."); return; }
        var types = hexPlacer.TileTypes;
        if (types == null || types.Length == 0) { Debug.LogError("Convert: HexPlacer.TileTypes is empty."); return; }

        int clamped = Mathf.Clamp(idx, 0, types.Length - 1);
        var selectedType = types[clamped];
        if (!selectedType) { Debug.LogWarning("Convert: selected TileType is null."); return; }

        if (!gm) gm = GameManager.Instance;
        if (!gm || !gm.TrySpend(playerKingdom, selectedType.Cost))
        {
            Debug.Log("Convert: not enough resources.");
            return;
        }

        ConvertTile(targetCell, selectedType, playerKingdom);
    }

    void ConvertTile(TileCell oldCell, TileType newType, Kingdom owner)
    {
        int x = oldCell.X, z = oldCell.Z;
        HexGrid grid = TileRegistry.GridRef;

        TileRegistry.Unregister(x, z, oldCell);
        Destroy(oldCell.gameObject);

        Vector3 centerLocal = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
        Vector3 worldPos = grid.transform.TransformPoint(centerLocal);

        GameObject go = Instantiate(newType.Prefab, worldPos, grid.transform.rotation, grid.transform);
        var newCell = go.GetComponent<TileCell>();
        if (!newCell) { Debug.LogError("Convert: new prefab missing TileCell."); Destroy(go); return; }

        newCell.InitFromType(newType);
        newCell.ForceSetOwner(owner);
        newCell.SetCoords(x, z, grid);

        if (newType.MaxPopulation > 0 && newType.InitialPopulation > 0 && gm != null)
        {
            int initial = Mathf.Clamp(newType.InitialPopulation, 0, newType.MaxPopulation);
            if (initial > 0) gm.AddTo(owner, new Resources { Population = initial });
        }

        RebuildBordersAround(x, z);
    }

    void RebuildBordersAround(int x, int z)
    {
        RebuildAt(x, z);
        for (int dir = 0; dir < 6; dir++)
        {
            var n = NeighborXZ(x, z, dir, TileRegistry.GridRef);
            RebuildAt(n.x, n.y);
        }
    }

    void RebuildAt(int x, int z)
    {
        if (TileRegistry.TryGetCell(x, z, out var c) && c)
            c.GetComponent<BorderPainter>()?.RebuildBorders();
    }

    bool EnsureEventSystem()
    {
        if (EventSystem.current != null) return true;
        Debug.LogError("TileConvertUI: No EventSystem in scene.");
        return false;
    }
    bool EnsureDropdownTemplate(TMP_Dropdown dd)
    {
        if (dd.template != null) return true;
        if (warnIfTemplateMissing)
            Debug.LogError("TileConvertUI: TMP_Dropdown.Template is not assigned.");
        return false;
    }
    bool EnsureHasOptions(TMP_Dropdown dd)
    {
        if (dd.options != null && dd.options.Count > 0) return true;
        Debug.LogError("TileConvertUI: TMP_Dropdown has no options.");
        return false;
    }

    // Neighbor math (same as before)
    static readonly Vector2Int[] P_ODD_R_EVEN = { new(+1, 0), new(0, +1), new(-1, +1), new(-1, 0), new(-1, -1), new(0, -1) };
    static readonly Vector2Int[] P_ODD_R_ODD = { new(+1, 0), new(+1, +1), new(0, +1), new(-1, 0), new(0, -1), new(+1, -1) };
    static readonly Vector2Int[] F_ODD_Q_EVEN = { new(0, +1), new(-1, 0), new(-1, -1), new(0, -1), new(+1, -1), new(+1, 0) };
    static readonly Vector2Int[] F_ODD_Q_ODD = { new(0, +1), new(-1, +1), new(-1, 0), new(0, -1), new(+1, 0), new(+1, +1) };

    static Vector2Int NeighborXZ(int x, int z, int dir, HexGrid grid)
    {
        if (grid.Orientation == HexGrid.HexOrientation.PointyTop)
        {
            var d = ((z & 1) == 0) ? P_ODD_R_EVEN[dir % 6] : P_ODD_R_ODD[dir % 6];
            return new Vector2Int(x + d.x, z + d.y);
        }
        else
        {
            var d = ((x & 1) == 0) ? F_ODD_Q_EVEN[dir % 6] : F_ODD_Q_ODD[dir % 6];
            return new Vector2Int(x + d.x, z + d.y);
        }
    }
}
