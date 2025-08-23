using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public enum HexOrientation { PointyTop, FlatTop }

    [field: SerializeField] public HexOrientation Orientation { get; private set; } = HexOrientation.PointyTop;
    [field: SerializeField] public int width { get; private set; } = 10;
    [field: SerializeField] public int height { get; private set; } = 10;
    [field: SerializeField] public float cellSize { get; private set; } = 1f;
    [field: SerializeField] public GameObject HexPrefab { get; private set; }

}
