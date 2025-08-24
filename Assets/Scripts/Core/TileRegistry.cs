using System.Collections.Generic;
using UnityEngine;

public static class TileRegistry
{
    // Active HexGrid reference (set by HexPlacer)
    public static HexGrid GridRef { get; private set; }

    // Active tiles by grid coord
    private static readonly Dictionary<Vector2Int, TileCell> cells = new();

    public static void SetGrid(HexGrid grid) => GridRef = grid;

    public static void Register(int x, int z, TileCell cell)
    {
        cells[new Vector2Int(x, z)] = cell;
    }

    public static void Unregister(int x, int z, TileCell cell)
    {
        var key = new Vector2Int(x, z);
        if (cells.TryGetValue(key, out var existing) && existing == cell)
            cells.Remove(key);
    }

    public static bool TryGetCell(int x, int z, out TileCell cell)
    {
        return cells.TryGetValue(new Vector2Int(x, z), out cell);
    }
}
