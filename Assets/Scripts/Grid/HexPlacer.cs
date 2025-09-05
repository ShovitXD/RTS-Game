using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class MapCell
{
    public int x;
    public int z;
    public int index;      // TileType index
    public int owner = -1; // kingdom owner; -1 means "absent" in old saves (back-compat)
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
    [SerializeField] public int index = 0; // selected tile type (via dropdown)
    [SerializeField] private KingdomSelector kingdomSelector;

    [Header("Context Menu UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform contextPanel;
    [SerializeField] private bool hideMenuOnLeftClick = true;

    [Header("Load Back-Compat")]
    [Tooltip("For old save files that have no owner field, use this owner when loading.")]
    [SerializeField] private Kingdom defaultLoadOwner = Kingdom.Player;

    // Expose last right-clicked tile for external UI (e.g., TileConvertUI)
    public static TileCell LastRightClickedCell { get; private set; }

    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    private readonly Dictionary<Vector2Int, int> placedIndex = new();

    private Vector2Int? currentCell = null;

    void Awake()
    {
        if (contextPanel) contextPanel.gameObject.SetActive(false);
        if (grid) TileRegistry.SetGrid(grid);
    }

    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (!hover || !grid) return;

        // === Left-click placement (DEV MODE ONLY via GameManager.DevMode) ===
        if (Input.GetMouseButtonDown(0))
        {
            if (hideMenuOnLeftClick) HideContextMenu();

            if (GameManager.Instance != null && GameManager.Instance.DevMode)
            {
                if (hover.TryGetHexUnderMouse(out int x, out int z))
                    PlaceOrReplaceAt(x, z, index);
            }
        }

        // === Right-click opens context menu ===
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

    // ===== Placement / Remove / Save / Load =====
    public void PlaceOrReplaceAt(int x, int z, int tileTypeIndex)
    {
        var k = kingdomSelector ? kingdomSelector.Current : Kingdom.Player;
        PlaceInternal(x, z, tileTypeIndex, k, skipCost: false);
    }

    void PlaceFromSave(int x, int z, int tileTypeIndex, Kingdom owner)
    {
        PlaceInternal(x, z, tileTypeIndex, owner, skipCost: true);
    }

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
            go = Instantiate(tt.Prefab, world, grid.transform.rotation, grid.transform);
            go.SetActive(false);
            placed[key] = go;
        }
        else go = existing;

        var cell = go.GetComponent<TileCell>();
        if (cell)
        {
            cell.InitFromType(tt);
            cell.SetCoords(x, z, grid);

            if (skipCost) cell.ForceSetOwner(owner);
            else if (!cell.TryPlace(owner))
            {
                go.SetActive(false);
                placedIndex.Remove(key);
                return;
            }
        }

        go.SetActive(true);
        placedIndex[key] = tileTypeIndex;

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

    public MapSnapshot CreateSnapshot()
    {
        var snap = new MapSnapshot { width = grid.width, height = grid.height };
        foreach (var kv in placedIndex)
        {
            var key = kv.Key;
            int typeIdx = kv.Value;

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
            Kingdom owner = (c.owner >= 0 && c.owner <= 5) ? (Kingdom)c.owner : defaultLoadOwner;
            PlaceFromSave(c.x, c.z, c.index, owner);
        }
    }

    // ===== Context Menu =====
    private void ShowContextMenu(Vector2 screenPos, Vector2Int cellCoord)
    {
        currentCell = cellCoord;

        LastRightClickedCell = null;
        if (TileRegistry.TryGetCell(cellCoord.x, cellCoord.y, out var c) && c && c.gameObject.activeSelf)
            LastRightClickedCell = c;

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
        LastRightClickedCell = null;
        if (contextPanel) contextPanel.gameObject.SetActive(false);
    }

    public void UI_CloseMenu() => HideContextMenu();

    // ===== Helpers =====
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
            var even = new[] { new Vector2Int(+1, 0), new Vector2Int(0, +1), new Vector2Int(-1, +1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1) };
            var odd = new[] { new Vector2Int(+1, 0), new Vector2Int(+1, +1), new Vector2Int(0, +1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(+1, -1) };
            var d = ((z & 1) == 0) ? even[dir % 6] : odd[dir % 6];
            return new Vector2Int(x + d.x, z + d.y);
        }
        else
        {
            var even = new[] { new Vector2Int(0, +1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(+1, -1), new Vector2Int(+1, 0) };
            var odd = new[] { new Vector2Int(0, +1), new Vector2Int(-1, +1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(+1, 0), new Vector2Int(+1, +1) };
            var d = ((x & 1) == 0) ? even[dir % 6] : odd[dir % 6];
            return new Vector2Int(x + d.x, z + d.y);
        }
    }
}
