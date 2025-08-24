using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class MapCell
{
    public int x;
    public int z;
    public int index;     // TileType index
    public int owner = -1; // NEW: kingdom owner; -1 means "not present in old saves"
}

[System.Serializable]
public class MapSnapshot
{
    public int width;
    public int height;
    public List<MapCell> cells = new();
}

public class HexPlacer : MonoBehaviour
{
    [Header("Grid & Hover")]
    [SerializeField] public Gizmo hover;
    [SerializeField] public HexGrid grid;

    [Header("Tile Types (data assets)")]
    [SerializeField] private TileType[] tileTypes;
    public TileType[] TileTypes => tileTypes;

    [Header("Selection")]
    [SerializeField] public int index = 0;                 // selected tile type (via dropdown)
    [SerializeField] private KingdomSelector kingdomSelector;

    [Header("Context Menu UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform contextPanel;
    [SerializeField] private bool hideMenuOnLeftClick = true;

    [Header("Load Back-Compat")]
    [Tooltip("For old save files that have no owner field, use this owner when loading.")]
    [SerializeField] private Kingdom defaultLoadOwner = Kingdom.Player;

    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    private readonly Dictionary<Vector2Int, int> placedIndex = new();

    private Vector2Int? currentCell = null;

    // Neighbor offsets (Pointy = odd-r, Flat = odd-q)
    static readonly Vector2Int[] P_ODD_R_EVEN = { new(+1, 0), new(0, +1), new(-1, +1), new(-1, 0), new(-1, -1), new(0, -1) };
    static readonly Vector2Int[] P_ODD_R_ODD = { new(+1, 0), new(+1, +1), new(0, +1), new(-1, 0), new(0, -1), new(+1, -1) };
    static readonly Vector2Int[] F_ODD_Q_EVEN = { new(0, +1), new(-1, 0), new(-1, -1), new(0, -1), new(+1, -1), new(+1, 0) };
    static readonly Vector2Int[] F_ODD_Q_ODD = { new(0, +1), new(-1, +1), new(-1, 0), new(0, -1), new(+1, 0), new(+1, +1) };

    void Awake()
    {
        if (contextPanel) contextPanel.gameObject.SetActive(false);
        if (grid) TileRegistry.SetGrid(grid);
    }

    void Update()
    {
        // Block input when the pointer is over any UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!hover || !grid) return;

        // Left-click: place/replace with current selection & current kingdom
        if (Input.GetMouseButtonDown(0))
        {
            if (hideMenuOnLeftClick) HideContextMenu();
            if (hover.TryGetHexUnderMouse(out int x, out int z))
                PlaceOrReplaceAt(x, z, index);
        }

        // Right-click: open menu if there is an ACTIVE tile in that cell
        if (Input.GetMouseButtonDown(1))
        {
            if (hover.TryGetHexUnderMouse(out int x, out int z))
            {
                var key = new Vector2Int(x, z);
                if (placed.TryGetValue(key, out var go) && go && go.activeSelf)
                    ShowContextMenu(Input.mousePosition, key);
                else
                    HideContextMenu();
            }
            else HideContextMenu();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) HideContextMenu();
    }

    // === Placement (editor/player) — uses current dropdown owner & pays cost ===
    public void PlaceOrReplaceAt(int x, int z, int tileTypeIndex)
    {
        var k = kingdomSelector ? kingdomSelector.Current : Kingdom.Player;
        PlaceInternal(x, z, tileTypeIndex, k, skipCost: false);
    }

    // === Placement for LOAD — owner comes from save; no cost ===
    void PlaceFromSave(int x, int z, int tileTypeIndex, Kingdom owner)
    {
        PlaceInternal(x, z, tileTypeIndex, owner, skipCost: true);
    }

    // Core place logic shared by both code paths
    void PlaceInternal(int x, int z, int tileTypeIndex, Kingdom owner, bool skipCost)
    {
        if (tileTypes == null || tileTypeIndex < 0 || tileTypeIndex >= tileTypes.Length) return;
        var tt = tileTypes[tileTypeIndex]; if (!tt || !tt.Prefab) return;

        var key = new Vector2Int(x, z);

        placed.TryGetValue(key, out var existing);
        bool hasActive = placedIndex.TryGetValue(key, out int existingIndex);
        bool needsNewInstance = (existing == null) || (hasActive && existingIndex != tileTypeIndex);

        GameObject go;
        if (needsNewInstance)
        {
            if (existing) Destroy(existing);

            Vector3 local = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
            Vector3 world = grid.transform.TransformPoint(local);

            go = Instantiate(tt.Prefab, world, grid.transform.rotation);
            go.transform.SetParent(grid.transform, true);
            go.SetActive(false);

            placed[key] = go;
        }
        else
        {
            go = existing;
            if (go == null) return;
        }

        var cell = go.GetComponent<TileCell>();
        if (cell)
        {
            cell.InitFromType(tt);
            cell.SetCoords(x, z, grid);

            if (skipCost)
            {
                // Loading: set owner directly, no payment
                cell.ForceSetOwner(owner);
            }
            else
            {
                // Live placement: pay & set owner
                if (!cell.TryPlace(owner))
                {
                    go.SetActive(false);
                    placedIndex.Remove(key);
                    return;
                }
            }
        }

        // Activate and record type
        go.SetActive(true);
        placedIndex[key] = tileTypeIndex;

        // Rebuild borders for this cell + neighbors
        go.GetComponent<BorderPainter>()?.RebuildBorders();
        RebuildNeighbors(x, z);
    }

    public bool TryRemoveAt(int x, int z)
    {
        var key = new Vector2Int(x, z);
        if (!placed.TryGetValue(key, out var go) || !go) return false;

        go.SetActive(false);
        placedIndex.Remove(key);

        RebuildNeighbors(x, z);
        return true;
    }

    public void ClearAll()
    {
        foreach (var kv in placed) if (kv.Value) kv.Value.SetActive(false);
        placedIndex.Clear();
        HideContextMenu();
    }

    // === Save/Load (now saves owner as well) ===
    public MapSnapshot CreateSnapshot()
    {
        var snap = new MapSnapshot { width = grid.width, height = grid.height };

        foreach (var kv in placedIndex)
        {
            var key = kv.Key;
            int typeIdx = kv.Value;

            // read owner from the active TileCell
            Kingdom owner = Kingdom.Player;
            if (placed.TryGetValue(key, out var go) && go)
            {
                var cell = go.GetComponent<TileCell>();
                if (cell) owner = cell.Owner;
            }

            snap.cells.Add(new MapCell { x = key.x, z = key.y, index = typeIdx, owner = (int)owner });
        }
        return snap;
    }

    public void ApplySnapshot(MapSnapshot snap)
    {
        if (snap == null) return;
        ClearAll();

        foreach (var c in snap.cells)
        {
            // Backward compatibility: old saves have owner = -1
            Kingdom owner = (c.owner >= 0 && c.owner <= 5) ? (Kingdom)c.owner : defaultLoadOwner;
            PlaceFromSave(c.x, c.z, c.index, owner);
        }
    }

    // === Context Menu UI ===
    private void ShowContextMenu(Vector2 screenPos, Vector2Int cell)
    {
        currentCell = cell;
        if (!uiCanvas || !contextPanel) return;

        RectTransform canvasRect = uiCanvas.transform as RectTransform;
        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out var localPoint))
        {
            contextPanel.anchoredPosition = localPoint;
            contextPanel.gameObject.SetActive(true);
        }
    }

    private void HideContextMenu()
    {
        currentCell = null;
        if (contextPanel) contextPanel.gameObject.SetActive(false);
    }

    public void UI_DeleteTile()
    {
        if (currentCell.HasValue)
            TryRemoveAt(currentCell.Value.x, currentCell.Value.y);
        HideContextMenu();
    }

    public void UI_CloseMenu()
    {
        HideContextMenu();
    }

    // --- Helpers ---
    void RebuildNeighbors(int x, int z)
    {
        for (int dir = 0; dir < 6; dir++)
        {
            var nz = NeighborXZ_Local(x, z, dir, grid);
            if (TileRegistry.TryGetCell(nz.x, nz.y, out var nCell) && nCell && nCell.gameObject.activeSelf)
                nCell.GetComponent<BorderPainter>()?.RebuildBorders();
        }
    }

    static Vector2Int NeighborXZ_Local(int x, int z, int dir, HexGrid grid)
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
