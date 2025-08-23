using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class KingdomSelector : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;

    // Dropdown: 0=Player, 1=Enemy, 2=Friendly, 3=Faction3, 4=Faction4, 5=Unowned
    public Kingdom Current
    {
        get
        {
            if (!dropdown) return Kingdom.Player;
            switch (dropdown.value)
            {
                case 0: return Kingdom.Player;
                case 1: return Kingdom.Enemy;
                case 2: return Kingdom.Friendly;
                case 3: return Kingdom.Faction3;
                case 4: return Kingdom.Faction4;
                case 5: return Kingdom.None;
                default: return Kingdom.Player;
            }
        }
    }

    void Awake()
    {
        if (!dropdown) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Player"),
            new TMP_Dropdown.OptionData("Enemy"),
            new TMP_Dropdown.OptionData("Friendly"),
            new TMP_Dropdown.OptionData("Faction 3"),
            new TMP_Dropdown.OptionData("Faction 4"),
            new TMP_Dropdown.OptionData("Unowned")
        });
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
    }
}
