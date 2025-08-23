using UnityEngine;

[ExecuteAlways]
public class Gizmo : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] HexGrid grid;

    void OnDrawGizmos()
    {
        if (!grid || grid.width <= 0 || grid.height <= 0 || grid.cellSize <= 0f) return;

        var cornersLocal = HexMatrix.Corners(grid.cellSize, grid.Orientation);

        Gizmos.color = Color.gray;
        for (int z = 0; z < grid.height; z++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                var centerLocal = HexMatrix.Center(grid.cellSize, x, z, grid.Orientation);
                DrawHex(centerLocal, cornersLocal);
            }
        }

        if (Application.isPlaying && TryGetHexUnderMouse(out int hx, out int hz))
        {
            Gizmos.color = Color.yellow;
            var centerLocal = HexMatrix.Center(grid.cellSize, hx, hz, grid.Orientation);
            DrawHex(centerLocal, cornersLocal);
        }
    }

    void DrawHex(Vector3 centerLocal, Vector3[] cornersLocal)
    {
        for (int s = 0; s < 6; s++)
        {
            Vector3 a = transform.TransformPoint(centerLocal + cornersLocal[s]);
            Vector3 b = transform.TransformPoint(centerLocal + cornersLocal[(s + 1) % 6]);
            Gizmos.DrawLine(a, b);
        }
    }

    public bool TryGetHexUnderMouse(out int col, out int row)
    {
        col = row = -1;
        if (!cam || !grid) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(grid.transform.up, grid.transform.position);
        if (!plane.Raycast(ray, out float t)) return false;

        Vector3 hitWorld = ray.GetPoint(t);
        Vector3 p = grid.transform.InverseTransformPoint(hitWorld);

        float s = grid.cellSize;
        const float sqrt3 = 1.7320508075688772f;

        float qf, rf;
        if (grid.Orientation == HexGrid.HexOrientation.PointyTop)
        {
            qf = (sqrt3 / 3f * p.x - 1f / 3f * p.z) / s;
            rf = (2f / 3f * p.z) / s;
        }
        else
        {
            qf = (2f / 3f * p.x) / s;
            rf = (-1f / 3f * p.x + (sqrt3 / 3f) * p.z) / s;
        }

        Axial a = RoundAxial(qf, rf);

        if (grid.Orientation == HexGrid.HexOrientation.PointyTop)
        {
            col = a.q + ((a.r - (a.r & 1)) >> 1);
            row = a.r;
        }
        else
        {
            col = a.q;
            row = a.r + ((a.q - (a.q & 1)) >> 1);
        }

        if (col < 0 || col >= grid.width || row < 0 || row >= grid.height) return false;
        return true;
    }

    struct Axial { public int q, r; public Axial(int q, int r) { this.q = q; this.r = r; } }

    static Axial RoundAxial(float qf, float rf)
    {
        float xf = qf, zf = rf, yf = -xf - zf;
        int xi = Mathf.RoundToInt(xf);
        int yi = Mathf.RoundToInt(yf);
        int zi = Mathf.RoundToInt(zf);

        float dx = Mathf.Abs(xi - xf);
        float dy = Mathf.Abs(yi - yf);
        float dz = Mathf.Abs(zi - zf);

        if (dx > dy && dx > dz) xi = -yi - zi;
        else if (dy > dz) yi = -xi - zi;
        else zi = -xi - yi;

        return new Axial(xi, zi);
    }
}
