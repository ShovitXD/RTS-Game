using System.IO;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] HexPlacer placer;

    [Header("File")]
    [SerializeField] string fileName = "map_snapshot.json";

    [Header("Runtime")]
    [SerializeField] bool autoLoadOnStart = true;

    void Start()
    {
        if (autoLoadOnStart)
            LoadFromDisk();
    }

    string GetDefaultDirectory()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../SavedMaps"));
    }

    string GetDefaultPath()
    {
        return Path.Combine(GetDefaultDirectory(), fileName);
    }

    public void SaveToDisk()
    {
        if (!placer) { Debug.LogWarning("MapManager: HexPlacer reference missing."); return; }
        var snap = placer.CreateSnapshot();
        var json = JsonUtility.ToJson(snap, true);

        string dir = GetDefaultDirectory();
        Directory.CreateDirectory(dir);
        string path = GetDefaultPath();

        File.WriteAllText(path, json);
        Debug.Log($"Map saved: {path}");
    }

    public void LoadFromDisk()
    {
        if (!placer) { Debug.LogWarning("MapManager: HexPlacer reference missing."); return; }

        string path = GetDefaultPath();
        if (!File.Exists(path))
        {
            Debug.LogWarning($"MapManager: No saved map at {path}");
            return;
        }

        string json = File.ReadAllText(path);
        var snap = JsonUtility.FromJson<MapSnapshot>(json);
        placer.ApplySnapshot(snap);
        Debug.Log($"Map loaded: {path}");
    }
}
