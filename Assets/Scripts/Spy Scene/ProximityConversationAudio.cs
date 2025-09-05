using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Looping3DAudio : MonoBehaviour
{
    [Min(0.1f)] public float innerFullVolume = 2f; // AudioSource.minDistance
    [Min(0.5f)] public float outerRange = 12f;     // AudioSource.maxDistance

    void Awake()
    {
        var src = GetComponent<AudioSource>();
        // Assign your clip in the Inspector.
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = innerFullVolume;
        src.maxDistance = outerRange;
        src.loop = true;
        src.playOnAwake = true; // starts immediately and never stops
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        var src = GetComponent<AudioSource>();
        if (!src) return;
        src.minDistance = innerFullVolume;
        src.maxDistance = outerRange;
    }
#endif
}
