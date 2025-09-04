using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BorderPainter : MonoBehaviour
{
    // Auto-fetched refs
    private TileCell cell;
    private HexGrid grid;

    [Header("Edge Strip Prefab (NO material)")]
    [Tooltip("Prefab with an EdgeStrip component; thin quad/mesh. Local Z = length, X = thickness, Y = height.")]
    [SerializeField] private GameObject edgeStripPrefab;

    [Header("Strip Size & Placement")]
    [Tooltip("Local Y scale (height) of the strip.")]
    [SerializeField, Min(0f)] private float heightY = 0.1f;
    [Tooltip("Local X scale (thickness) of the strip.")]
    [SerializeField, Min(0f)] private float thicknessX = 0.05f;
    [Tooltip("Lift above the ground to avoid z-fighting.")]
    [SerializeField, Min(0f)] private float yLift = 0.01f;
    [Tooltip("Inset amount: pulls the border inward from the hex edge (in world units along the edge normal).")]
    [SerializeField, Min(0f)] private float lateralOffset = 0.05f;

    [Header("Per-Kingdom Materials")]
    [SerializeField] private Material playerMat;
    [SerializeField] private Material enemyMat;
    [SerializeField] private Material friendlyMat;
    [SerializeField] private Material faction3Mat;
    [SerializeField] private Material faction4Mat;
    // Unowned/None => no border

    [Header("Rules")]
    [Tooltip("If true, draw borders only when the neighbor is owned by a (different) kingdom; no borders against empty cells.")]
    [SerializeField] private bool drawOnlyIfNeighborOwned = true;

    // one strip per edge index (0..5)
    private readonly Dictionary<int, GameObject> edgeObjs = new Dictionary<int, GameObject>(6);

    void Awake()
    {
        cell = GetComponent<TileCell>();
        grid = TileRegistry.GridRef;
    }

    void OnEnable()
    {
        if (cell != null && cell.HasCoords) RebuildBorders();
    }

    public void RebuildBorders()
    {
        if (!isActiveAndEnabled) return;
        if (cell == null || grid == null || edgeStripPrefab == null) return;
        if (!cell.HasCoords) return;

        var myK = cell.Owner;
        if (myK == Kingdom.None) { HideAllEdges(); return; }

        bool[] edgeIsBoundary = new bool[6];
        Vector2Int[] neighborXZs = new Vector2Int[6];

        for (int dir = 0; dir < 6; dir++)
        {
            var nz = NeighborXZ(cell.X, cell.Z, dir, grid);
            neighborXZs[dir] = nz;

            if (TileRegistry.TryGetCell(nz.x, nz.y, out var nCell) && nCell && nCell.gameObject.activeSelf)
            {
                edgeIsBoundary[dir] = drawOnlyIfNeighborOwned
                    ? (nCell.Owner != Kingdom.None && nCell.Owner != myK)
                    : (nCell.Owner != myK);
            }
            else
            {
                edgeIsBoundary[dir] = false;
            }
        }

        Vector3 localCenter = HexMatrix.Center(grid.cellSize, cell.X, cell.Z, grid.Orientation);
        Vector3[] localCorners = HexMatrix.Corners(grid.cellSize, grid.Orientation);
        Vector3 centerWorld = grid.transform.TransformPoint(localCenter);
        Material mat = GetMaterialFor(myK);

        for (int e = 0; e < 6; e++)
        {
            if (!edgeIsBoundary[e])
            {
                SetEdgeActive(e, false);
                continue;
            }

            // Neighbor center (world)
            Vector3 nCenterLocal = HexMatrix.Center(grid.cellSize, neighborXZs[e].x, neighborXZs[e].y, grid.Orientation);
            Vector3 nCenterWorld = grid.transform.TransformPoint(nCenterLocal);

            // Find the edge of THIS hex that faces the neighbor
            (int cA, int cB) = FindEdgeFacingNeighbor(centerWorld, localCenter, localCorners, grid.transform, nCenterWorld);

            // --- Inset the edge toward the cell center (shrink corner ring) ---
            Vector3 vA = localCorners[cA]; vA.y = 0f;
            Vector3 vB = localCorners[cB]; vB.y = 0f;

            float magA = vA.magnitude;
            float magB = vB.magnitude;

            // Clamp inset so we don't cross the center
            float insetA = Mathf.Min(lateralOffset, Mathf.Max(0f, magA - 1e-4f));
            float insetB = Mathf.Min(lateralOffset, Mathf.Max(0f, magB - 1e-4f));

            Vector3 aLocalInset = localCenter + (magA > 1e-6f ? (vA - vA.normalized * insetA) : vA);
            Vector3 bLocalInset = localCenter + (magB > 1e-6f ? (vB - vB.normalized * insetB) : vB);

            // World endpoints
            Vector3 aW = grid.transform.TransformPoint(aLocalInset);
            Vector3 bW = grid.transform.TransformPoint(bLocalInset);

            // Small vertical lift
            aW.y += yLift;
            bW.y += yLift;

            // Ensure/activate the strip object
            if (!edgeObjs.TryGetValue(e, out var stripGO) || !stripGO)
            {
                stripGO = Instantiate(edgeStripPrefab);
                stripGO.transform.SetParent(transform, false);
                edgeObjs[e] = stripGO;
            }
            if (!stripGO.activeSelf) stripGO.SetActive(true);

            var strip = stripGO.GetComponent<EdgeStrip>();
            if (!strip) strip = stripGO.AddComponent<EdgeStrip>();
            strip.Configure(aW, bW, mat, thicknessX, heightY);
        }
    }

    private void SetEdgeActive(int e, bool on)
    {
        if (edgeObjs.TryGetValue(e, out var go) && go)
            go.SetActive(on);
    }

    private void HideAllEdges()
    {
        foreach (var kv in edgeObjs)
            if (kv.Value) kv.Value.SetActive(false);
    }

    private Material GetMaterialFor(Kingdom k)
    {
        switch (k)
        {
            case Kingdom.Player: return playerMat;
            case Kingdom.Enemy: return enemyMat;
            case Kingdom.Friendly: return friendlyMat;
            case Kingdom.Faction3: return faction3Mat;
            case Kingdom.Faction4: return faction4Mat;
            default: return null;
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

    // --- Edge finder (uses localCenter when transforming corners) ---
    static (int a, int b) FindEdgeFacingNeighbor(
        Vector3 centerW,
        Vector3 localCenter,
        Vector3[] localCorners,
        Transform gridTf,
        Vector3 neighborCenterW)
    {
        // Build world-space corner ring for THIS cell (center + corners)
        Vector3[] cw = new Vector3[6];
        for (int i = 0; i < 6; i++)
            cw[i] = gridTf.TransformPoint(localCenter + localCorners[i]);

        Vector3 toNeighbor = neighborCenterW - centerW; toNeighbor.y = 0f;
        if (toNeighbor.sqrMagnitude < 1e-8f) return (0, 1);
        toNeighbor.Normalize();

        float bestDot = float.NegativeInfinity;
        int bestA = 0, bestB = 1;
        for (int i = 0; i < 6; i++)
        {
            int j = (i + 1) % 6;
            Vector3 mid = 0.5f * (cw[i] + cw[j]);
            Vector3 dir = mid - centerW; dir.y = 0f;
            float d = Vector3.Dot(dir.normalized, toNeighbor);
            if (d > bestDot) { bestDot = d; bestA = i; bestB = j; }
        }
        return (bestA, bestB);
    }
}
