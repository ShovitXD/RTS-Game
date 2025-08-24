using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable] public class MapCell { public int x, z, index; }
[System.Serializable] public class MapSnapshot { public int width, height; public List<MapCell> cells = new(); }

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
    [SerializeField] private Canvas uiCanvas;              // your UI Canvas
    [SerializeField] private RectTransform contextPanel;   // panel with Delete + Close buttons
    [SerializeField] private bool hideMenuOnLeftClick = true;

    // Per-cell spawned object we keep around (may be inactive)
    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    // Active tile type index per cell (only recorded when GO is active)
    private readonly Dictionary<Vector2Int, int> placedIndex = new();

    // state for open context menu
    private Vector2Int? currentCell = null;

    void Awake()
    {
        if (contextPanel) contextPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        // Block input when the pointer is over any UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!hover || !grid) return;

        // Left-click: place/replace
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

    // === Placement (lazy activation, but swap prefab if type changed) ===
    public void PlaceOrReplaceAt(int x, int z, int tileTypeIndex)
    {
        if (tileTypes == null || tileTypeIndex < 0 || tileTypeIndex >= tileTypes.Length) return;
        var tt = tileTypes[tileTypeIndex]; if (!tt || !tt.Prefab) return;

        var key = new Vector2Int(x, z);

        // Determine if we need a NEW instance (type changed) or can reuse existing
        placed.TryGetValue(key, out var existing);
        bool hasActive = placedIndex.TryGetValue(key, out int existingIndex);
        bool needsNewInstance = (existing == null) || (hasActive && existingIndex != tileTypeIndex);

        GameObject go;
        if (needsNewInstance)
        {
            // Destroy old if present (active or inactive) when type differs
            if (existing) Destroy(existing);

            // Instantiate a fresh prefab of the requested type at the cell center
            Vector3 local = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
            Vector3 world = grid.transform.TransformPoint(local);

            go = Instantiate(tt.Prefab, world, grid.transform.rotation);
            go.transform.SetParent(grid.transform, true);
            go.SetActive(false); // activate only after successful placement

            placed[key] = go;
        }
        else
        {
            // Reuse existing instance (same type)
            go = existing;
            if (go == null) return; // safety
        }

        // Initialize tile + pay cost/assign owner
        var cell = go.GetComponent<TileCell>();
        if (cell)
        {
            cell.InitFromType(tt);
            var k = kingdomSelector ? kingdomSelector.Current : Kingdom.Player;
            if (!cell.TryPlace(k))
            {
                // Not enough resources (unless DevMode); keep it inactive & clear active record
                go.SetActive(false);
                placedIndex.Remove(key);
                return;
            }
        }

        // Success: activate and record active type index
        go.SetActive(true);
        placedIndex[key] = tileTypeIndex;
    }

    public bool TryRemoveAt(int x, int z)
    {
        var key = new Vector2Int(x, z);
        if (!placed.TryGetValue(key, out var go) || !go) return false;

        // Lazy delete: just deactivate; keep object for possible reuse later
        go.SetActive(false);
        placedIndex.Remove(key);
        return true;
    }

    public void ClearAll()
    {
        foreach (var kv in placed) if (kv.Value) kv.Value.SetActive(false);
        placedIndex.Clear();
        HideContextMenu();
    }

    // === Save/Load (only saves ACTIVE tiles) ===
    public MapSnapshot CreateSnapshot()
    {
        var snap = new MapSnapshot { width = grid.width, height = grid.height };
        foreach (var kv in placedIndex)
            snap.cells.Add(new MapCell { x = kv.Key.x, z = kv.Key.y, index = kv.Value });
        return snap;
    }

    public void ApplySnapshot(MapSnapshot snap)
    {
        if (snap == null) return;
        ClearAll();
        foreach (var c in snap.cells)
            PlaceOrReplaceAt(c.x, c.z, c.index);
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

    // --- Hook these to the two UI buttons on your panel ---
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
}
