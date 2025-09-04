using UnityEngine;

/// <summary>
/// Poses a thin quad/mesh between two world points.
/// Assumes prefab's local Z = length, X = thickness, Y = height.
/// </summary>
[DisallowMultipleComponent]
public class EdgeStrip : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer; // optional; auto-finds if null

    public void Configure(Vector3 aWorld, Vector3 bWorld, Material mat, float thicknessX, float heightY)
    {
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer && mat) targetRenderer.sharedMaterial = mat;

        // length & direction
        Vector3 dir = bWorld - aWorld;
        float len = dir.magnitude;
        if (len < 1e-5f) { gameObject.SetActive(false); return; }
        dir /= len;

        // position at midpoint

        Vector3 mid = 0.5f * (aWorld + bWorld);
        transform.position = mid;

        // face along edge in XZ plane
        Vector3 fwd = dir; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

        // scale: X = thickness, Y = height, Z = length
        var s = transform.localScale;
        s.x = thicknessX;
        s.y = heightY;
        s.z = len;
        transform.localScale = s;
    }
}
