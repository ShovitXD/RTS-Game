// Scripts/Core/TileRegistry.cs
using System.Collections.Generic;
using UnityEngine;

public static class TileRegistry
{
    private static readonly Dictionary<Vector2Int, TileCell> cells = new Dictionary<Vector2Int, TileCell>(256);

    public static HexGrid GridRef { get; private set; }
    public static IEnumerable<TileCell> AllCells => cells.Values;
    public static int Count => cells.Count;

    public static void SetGrid(HexGrid grid) => GridRef = grid;

    public static void Register(int x, int z, TileCell cell)
    {
        cells[new Vector2Int(x, z)] = cell;
    }

    public static void Unregister(int x, int z, TileCell cell)
    {
        var k = new Vector2Int(x, z);
        if (cells.TryGetValue(k, out var existing) && existing == cell)
            cells.Remove(k);
    }

    public static bool TryGetCell(int x, int z, out TileCell cell)
        => cells.TryGetValue(new Vector2Int(x, z), out cell);

    public static void Clear()
    {
        cells.Clear();
        GridRef = null;
    }
}
