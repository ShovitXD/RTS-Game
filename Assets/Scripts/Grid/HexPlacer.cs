using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class MapCell { public int x, z, index; }
[System.Serializable] public class MapSnapshot { public int width, height; public List<MapCell> cells = new(); }

public class HexPlacer : MonoBehaviour
{
    [SerializeField] public Gizmo hover;
    [SerializeField] public HexGrid grid;

    [Header("Tile Types (data assets)")]
    [SerializeField] private TileType[] tileTypes;
    public TileType[] TileTypes => tileTypes;

    [Header("Selection")]
    [SerializeField] public int index = 0;              // chosen via dropdown
    [SerializeField] private KingdomSelector kingdomSelector; // UI Kingdom pick

    private readonly Dictionary<Vector2Int, GameObject> placed = new();
    private readonly Dictionary<Vector2Int, int> placedIndex = new();

    void Update()
    {
        if (!hover || !grid) return;
        if (Input.GetMouseButtonDown(0) && hover.TryGetHexUnderMouse(out int x, out int z))
            PlaceOrReplaceAt(x, z, index);
    }

    public void PlaceOrReplaceAt(int x, int z, int tileTypeIndex)
    {
        if (tileTypes == null || tileTypeIndex < 0 || tileTypeIndex >= tileTypes.Length) return;
        var tt = tileTypes[tileTypeIndex]; if (!tt || !tt.Prefab) return;

        var key = new Vector2Int(x, z);
        if (placed.TryGetValue(key, out var existing) && existing) Destroy(existing);

        Vector3 local = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
        Vector3 world = grid.transform.TransformPoint(local);

        var go = Instantiate(tt.Prefab, world, grid.transform.rotation);
        go.transform.SetParent(grid.transform, true);

        var cell = go.GetComponent<TileCell>();
        if (cell)
        {
            cell.InitFromType(tt);
            var k = kingdomSelector ? kingdomSelector.Current : Kingdom.Player;
            if (!cell.TryPlace(k))
            {
                Destroy(go); // not enough resources (unless DevMode)
                return;
            }
        }

        placed[key] = go;
        placedIndex[key] = tileTypeIndex;
    }

    public void ClearAll()
    {
        foreach (var kv in placed) if (kv.Value) DestroyImmediate(kv.Value);
        placed.Clear(); placedIndex.Clear();
    }

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
}
