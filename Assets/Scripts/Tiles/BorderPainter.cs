using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BorderPainter : MonoBehaviour
{
    // Auto-fetched refs
    private TileCell cell;
    private HexGrid grid;

    [Header("Border")]
    [SerializeField] private GameObject borderPolePrefab;   // assign in prefab
    [Tooltip("Nudge poles slightly inward so they don't overlap neighbors")]
    [SerializeField] private float inwardOffset = 0.05f;

    // spawned poles per corner index (0..5)
    private readonly Dictionary<int, GameObject> cornerPoles = new Dictionary<int, GameObject>(6);

    void Awake()
    {
        cell = GetComponent<TileCell>();
        grid = TileRegistry.GridRef;
    }

    void OnEnable()
    {
        if (cell != null && cell.HasCoords) RebuildBorders();
    }

    /// <summary>
    /// Show poles at corners that touch an existing neighbor with a different kingdom.
    /// Hide poles for same-kingdom neighbors or empty neighbors.
    /// </summary>
    public void RebuildBorders()
    {
        if (!isActiveAndEnabled) return;
        if (cell == null || grid == null || borderPolePrefab == null) return;
        if (!cell.HasCoords) return;

        var myK = cell.Owner;
        bool[] edgeIsBoundary = new bool[6];

        // An edge is a boundary ONLY if there IS a neighbor AND its owner differs.
        for (int dir = 0; dir < 6; dir++)
        {
            var nz = NeighborXZ(cell.X, cell.Z, dir, grid);
            if (TileRegistry.TryGetCell(nz.x, nz.y, out var nCell) && nCell && nCell.gameObject.activeSelf)
            {
                edgeIsBoundary[dir] = (nCell.Owner != myK);
            }
            else
            {
                edgeIsBoundary[dir] = false; // empty neighbor => NO pole (per your request)
            }
        }

        // Local center + corners of this hex
        var localCenter = HexMatrix.Center(grid.cellSize, cell.X, cell.Z, grid.Orientation);
        var localCorners = HexMatrix.Corners(grid.cellSize, grid.Orientation);

        // Corner c adjacency depends on orientation:
        // - PointyTop: corner c is between edges c and (c+1)%6
        // - FlatTop : corner c is between edges (c+5)%6 and c
        for (int c = 0; c < 6; c++)
        {
            int eA, eB;
            if (grid.Orientation == HexGrid.HexOrientation.PointyTop)
            {
                eA = c;
                eB = (c + 1) % 6;
            }
            else // FlatTop
            {
                eA = (c + 5) % 6;
                eB = c;
            }

            bool cornerOnBoundary = edgeIsBoundary[eA] || edgeIsBoundary[eB];

            if (cornerOnBoundary)
            {
                if (!cornerPoles.TryGetValue(c, out var pole) || !pole)
                {
                    pole = Instantiate(borderPolePrefab, transform);
                    cornerPoles[c] = pole;
                }

                Vector3 cornerLocal = localCenter + localCorners[c];
                Vector3 centerWorld = grid.transform.TransformPoint(localCenter);
                Vector3 cornerWorld = grid.transform.TransformPoint(cornerLocal);

                // inward nudge so poles don't sit exactly on the edge
                Vector3 inward = (centerWorld - cornerWorld).normalized * inwardOffset;
                pole.transform.position = cornerWorld + inward;

                // Optional: rotate to face outward
                Vector3 outward = (cornerWorld - centerWorld);
                outward.y = 0f;
                if (outward.sqrMagnitude > 1e-6f)
                    pole.transform.rotation = Quaternion.LookRotation(outward.normalized, Vector3.up);

                if (!pole.activeSelf) pole.SetActive(true);
            }
            else
            {
                if (cornerPoles.TryGetValue(c, out var pole) && pole)
                    pole.SetActive(false);
            }
        }
    }

    // --- Neighbor offsets (Pointy = odd-r, Flat = odd-q) ---
    static readonly Vector2Int[] P_ODD_R_EVEN = // even row
    {
        new(+1,  0), new( 0, +1), new(-1, +1),
        new(-1,  0), new(-1, -1), new( 0, -1)
    };
    static readonly Vector2Int[] P_ODD_R_ODD = // odd row
    {
        new(+1,  0), new(+1, +1), new( 0, +1),
        new(-1,  0), new( 0, -1), new(+1, -1)
    };

    static readonly Vector2Int[] F_ODD_Q_EVEN = // even col
    {
        new( 0, +1), new(-1,  0), new(-1, -1),
        new( 0, -1), new(+1, -1), new(+1,  0)
    };
    static readonly Vector2Int[] F_ODD_Q_ODD = // odd col
    {
        new( 0, +1), new(-1, +1), new(-1,  0),
        new( 0, -1), new(+1,  0), new(+1, +1)
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
