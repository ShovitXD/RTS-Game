using UnityEngine;

public static class HexMatrix
{
    public static float OuterRadius(float hexSize) => hexSize;

    public static float InnerRadius(float hexSize) => hexSize * 0.866025404f; // sin(60°)

    public static Vector3 Corner(float hexSize, HexGrid.HexOrientation orientation, int index)
    {
        float angle = 60f * index;
        if (orientation == HexGrid.HexOrientation.PointyTop) angle += 30f;

        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(hexSize * Mathf.Cos(rad), 0f, hexSize * Mathf.Sin(rad));
    }

    public static Vector3[] Corners(float hexSize, HexGrid.HexOrientation orientation)
    {
        Vector3[] corners = new Vector3[6];
        for (int i = 0; i < 6; i++)
            corners[i] = Corner(hexSize, orientation, i);
        return corners;
    }

    public static Vector3 Center(float hexSize, int x, int z, HexGrid.HexOrientation orientation)
    {
        float sqrt3 = 1.7320508075688772f;
        Vector3 p = Vector3.zero;

        if (orientation == HexGrid.HexOrientation.PointyTop)
        {
            // Odd-r offset (rows). Shift odd rows by +0.5.
            float xOffset = ((z & 1) == 1) ? 0.5f : 0f;       // use 0f/0.5f; flip parity for Even-r
            p.x = hexSize * sqrt3 * (x + xOffset);
            p.z = hexSize * 1.5f * z;
        }
        else
        {
            // Odd-q offset (columns). Shift odd cols by +0.5.
            float zOffset = ((x & 1) == 1) ? 0.5f : 0f;       // flip parity for Even-q
            p.x = hexSize * 1.5f * x;
            p.z = hexSize * sqrt3 * (z + zOffset);
        }

        return p;
    }

}
