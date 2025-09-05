using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileConvertUI : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private GameManager gameManager;    // optional: auto-grab if empty
    [SerializeField] private HexPlacer hexPlacer;        // scene HexPlacer
    [SerializeField] private TMP_Dropdown tileDropdown;  // populated by TileDropdownBinder
    [SerializeField] private Kingdom playerKingdom = Kingdom.Player;

    // --- Optional: sanity checks to avoid first-time NREs ---
    [Header("Validation (optional)")]
    [SerializeField] private bool warnIfTemplateMissing = true;

    void Awake()
    {
        if (!gameManager) gameManager = GameManager.Instance;

        if (tileDropdown)
            tileDropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDestroy()
    {
        if (tileDropdown)
            tileDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    // === Button hook: called by the "Convert" button in the HexPlacer panel ===
    public void ShowDropdown()
    {
        if (!tileDropdown) { Debug.LogWarning("TileConvertUI: tileDropdown is not assigned."); return; }

        // 1) Basic guards (these are the usual causes of first-time NullReference in TMP_Dropdown.Show)
        if (!EnsureEventSystem()) return;
        if (!EnsureDropdownTemplate(tileDropdown)) return;
        if (!EnsureHasOptions(tileDropdown)) return;

        // 2) Make sure the GO is active before Show(), then show on next frame to avoid init race
        if (!tileDropdown.gameObject.activeSelf)
            tileDropdown.gameObject.SetActive(true);

        // Refresh layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tileDropdown.GetComponent<RectTransform>());

        // 3) Enable + interactable
        tileDropdown.enabled = true;
        tileDropdown.interactable = true;

        // 4) Defer Show() to the next frame — fixes first-time AlphaFadeList() NRE on some setups
        StartCoroutine(ShowNextFrame());
    }

    IEnumerator ShowNextFrame()
    {
        yield return null; // wait 1 frame so Template/Canvas fully initialize
        if (tileDropdown) tileDropdown.Show();
    }

    // === Button hook: also call this from your panel Close button ===
    public void HideDropdown()
    {
        if (!tileDropdown) return;
        tileDropdown.Hide(); // collapses if open
        tileDropdown.gameObject.SetActive(false);
    }

    // Optional legacy confirm button (if you keep one)
    public void OnClickConvert()
    {
        TryConvertToIndex(tileDropdown ? tileDropdown.value : 0);
    }

    private void OnDropdownChanged(int newIndex)
    {
        TryConvertToIndex(newIndex);

        // Auto-hide after conversion
        HideDropdown();
    }

    private void TryConvertToIndex(int idx)
    {
        var targetCell = HexPlacer.LastRightClickedCell;
        if (!targetCell || !targetCell.HasCoords) { Debug.Log("Convert: no target cell."); return; }

        // Rule: only convert your own tiles (change if needed)
        if (targetCell.Owner != playerKingdom) { Debug.Log("Convert: not your tile."); return; }

        if (!hexPlacer)
        {
            Debug.LogError("Convert: HexPlacer reference not assigned.");
            return;
        }
        var types = hexPlacer.TileTypes;
        if (types == null || types.Length == 0)
        {
            Debug.LogError("Convert: HexPlacer.TileTypes is empty.");
            return;
        }

        int clamped = Mathf.Clamp(idx, 0, types.Length - 1);
        var selectedType = types[clamped];
        if (!selectedType) { Debug.LogWarning("Convert: selected TileType is null."); return; }

        if (!gameManager || !gameManager.TrySpend(playerKingdom, selectedType.Cost))
        {
            Debug.Log("Convert: not enough resources.");
            return;
        }

        ConvertTile(targetCell, selectedType, playerKingdom);
    }

    private void ConvertTile(TileCell oldCell, TileType newType, Kingdom owner)
    {
        int x = oldCell.X;
        int z = oldCell.Z;
        HexGrid grid = TileRegistry.GridRef;

        // Unregister & destroy old
        TileRegistry.Unregister(x, z, oldCell);
        Destroy(oldCell.gameObject);

        // Spawn new
        Vector3 centerLocal = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
        Vector3 worldPos = grid.transform.TransformPoint(centerLocal);

        GameObject go = Instantiate(newType.Prefab, worldPos, grid.transform.rotation, grid.transform);
        var newCell = go.GetComponent<TileCell>();
        if (!newCell)
        {
            Debug.LogError("Convert: new prefab missing TileCell component.");
            Destroy(go);
            return;
        }

        // Initialize & register
        newCell.InitFromType(newType);
        newCell.ForceSetOwner(owner);  // cost already paid
        newCell.SetCoords(x, z, grid);

        // Grant initial population (since TryPlace() is not called)
        if (newType.MaxPopulation > 0 && newType.InitialPopulation > 0 && gameManager != null)
        {
            int initial = Mathf.Clamp(newType.InitialPopulation, 0, newType.MaxPopulation);
            if (initial > 0) gameManager.AddTo(owner, new Resources { Population = initial });
        }

        // Rebuild borders for this + neighbors
        RebuildBordersAround(x, z);
    }

    private void RebuildBordersAround(int x, int z)
    {
        RebuildAt(x, z);
        for (int dir = 0; dir < 6; dir++)
        {
            var n = NeighborXZ(x, z, dir, TileRegistry.GridRef);
            RebuildAt(n.x, n.y);
        }
    }

    private void RebuildAt(int x, int z)
    {
        if (TileRegistry.TryGetCell(x, z, out var c) && c)
            c.GetComponent<BorderPainter>()?.RebuildBorders();
    }

    // --- Guards / helpers -----------------------------------------------------

    bool EnsureEventSystem()
    {
        if (EventSystem.current != null) return true;
        Debug.LogError("TileConvertUI: No EventSystem in scene. Add one (GameObject → UI → Event System).");
        return false;
    }

    bool EnsureDropdownTemplate(TMP_Dropdown dd)
    {
        // TMP requires a valid Template + Item visuals — if missing, Show() can NRE the first time.
        if (dd.template != null) return true;

        if (warnIfTemplateMissing)
            Debug.LogError("TileConvertUI: TMP_Dropdown.Template is not assigned. Assign the Template (child with Viewport/Content/Item).");
        return false;
    }

    bool EnsureHasOptions(TMP_Dropdown dd)
    {
        if (dd.options != null && dd.options.Count > 0) return true;
        Debug.LogError("TileConvertUI: TMP_Dropdown has no options. Ensure TileDropdownBinder populated it before Show().");
        return false;
    }

    // Neighbor math (matches your hex systems)
    static readonly Vector2Int[] P_ODD_R_EVEN =
    {
        new(+1,0), new(0,+1), new(-1,+1), new(-1,0), new(-1,-1), new(0,-1)
    };
    static readonly Vector2Int[] P_ODD_R_ODD =
    {
        new(+1,0), new(+1,+1), new(0,+1), new(-1,0), new(0,-1), new(+1,-1)
    };
    static readonly Vector2Int[] F_ODD_Q_EVEN =
    {
        new(0,+1), new(-1,0), new(-1,-1), new(0,-1), new(+1,-1), new(+1,0)
    };
    static readonly Vector2Int[] F_ODD_Q_ODD =
    {
        new(0,+1), new(-1,+1), new(-1,0), new(0,-1), new(+1,0), new(+1,+1)
    };

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
