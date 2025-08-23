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

    // Per-cell spawned object (kept and toggled active/inactive)
    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    // Per-cell active tile type index (only for ACTIVE tiles)
    private readonly Dictionary<Vector2Int, int> placedIndex = new();

    // state for open context menu
    private Vector2Int? currentCell = null;

    void Awake()
    {
        if (contextPanel) contextPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        // ✅ If pointer is over any UI element, block placement/menu
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

        // Right-click: open menu if a tile exists at that cell
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

        // ESC closes menu
        if (Input.GetKeyDown(KeyCode.Escape)) HideContextMenu();
    }

    // === Placement (lazy activation) ===
    public void PlaceOrReplaceAt(int x, int z, int tileTypeIndex)
    {
        if (tileTypes == null || tileTypeIndex < 0 || tileTypeIndex >= tileTypes.Length) return;
        var tt = tileTypes[tileTypeIndex]; if (!tt || !tt.Prefab) return;

        var key = new Vector2Int(x, z);

        // Reuse existing GO if present; otherwise instantiate ONCE and keep for reuse
        if (!placed.TryGetValue(key, out var go) || !go)
        {
            Vector3 local = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
            Vector3 world = grid.transform.TransformPoint(local);

            go = Instantiate(tt.Prefab, world, grid.transform.rotation);
            go.transform.SetParent(grid.transform, true);
            go.SetActive(false); // stays inactive until placement confirmed

            placed[key] = go;
        }

        // Setup this tile instance with the selected type
        var cell = go.GetComponent<TileCell>();
        if (cell)
        {
            cell.InitFromType(tt);
            var k = kingdomSelector ? kingdomSelector.Current : Kingdom.Player;
            if (!cell.TryPlace(k))
            {
                // Not enough resources (unless DevMode); leave it inactive
                go.SetActive(false);
                placedIndex.Remove(key);
                return;
            }
        }

        // ✅ Activate after successful placement
        go.SetActive(true);
        placedIndex[key] = tileTypeIndex;
    }

    public bool TryRemoveAt(int x, int z)
    {
        var key = new Vector2Int(x, z);
        if (!placed.TryGetValue(key, out var go) || !go) return false;

        // Lazy delete: just deactivate; keep object for future reuse
        go.SetActive(false);
        placedIndex.Remove(key); // no active tile recorded for this cell
        return true;
    }

    public void ClearAll()
    {
        // Deactivate all and clear active indices; keep instances for reuse
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

        // Activate tiles described by snapshot; instantiate if cell has no instance yet
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
