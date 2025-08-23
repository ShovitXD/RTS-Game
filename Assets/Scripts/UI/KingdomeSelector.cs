using TMPro;
using UnityEngine;

public class KingdomSelector : MonoBehaviour
{
    [SerializeField] TMP_Dropdown dropdown;

    // Ensure dropdown options are 0..4 => Player, AI1..AI4
    public Kingdom Current
    {
        get
        {
            int v = dropdown ? dropdown.value : 0;
            return (Kingdom)Mathf.Clamp(v, 0, 4);
        }
    }
}
