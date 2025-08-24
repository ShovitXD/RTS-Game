using UnityEngine;

[CreateAssetMenu(fileName = "TileType", menuName = "RTS/Tile Type")]
public class TileType : ScriptableObject
{
    [Header("Prefab")]
    public GameObject Prefab;

    [Header("Placement Cost")]
    public Resources Cost;

    [Header("Default Per-Cell Values")]
    public Resources Values;   
}
