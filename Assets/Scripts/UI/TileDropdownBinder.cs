using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TileDropdownBinder : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] HexPlacer placer;     // now uses TileType[] + index

    void Awake()
    {
        if (!dropdown || !placer) return;

        var opts = new List<TMP_Dropdown.OptionData>();
        var types = placer.TileTypes;
        if (types != null)
        {
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                string name = t ? t.name : $"Empty {i}";
                opts.Add(new TMP_Dropdown.OptionData($"{i}: {name}"));
            }
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(opts);

        int initial = Mathf.Clamp(placer.index, 0, Mathf.Max(0, (types?.Length ?? 1) - 1));
        dropdown.SetValueWithoutNotify(initial);
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(v => placer.index = v);
    }

    void OnDestroy()
    {
        if (dropdown) dropdown.onValueChanged.RemoveAllListeners();
    }
}
