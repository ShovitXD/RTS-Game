using UnityEngine;

[CreateAssetMenu(fileName = "TileType", menuName = "RTS/Tile Type")]
public class TileType : ScriptableObject
{
    [Header("Prefab")]
    public GameObject Prefab;

    [Header("Placement Cost")]
    public Resources Cost;

    [Header("Default Per-Cell Resource Yields")]
    public Resources Values;   // Use Gold/Wood/Influence here; Population handled below

    [Header("Population Settings")]
    public int MaxPopulation = 0;        // cap per tile
    public int InitialPopulation = 0;    // granted on placement (and added to GameManager)
    public int PopulationPerTurn = 0;    // added each turn until Max
}
