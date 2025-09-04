using System;
using UnityEngine;

[Serializable]
public struct Resources
{
    public int Gold;
    public int Wood;
    public int Influence;
    public int Population;   // keep this so GameManager can track totals/UI/events

    public static readonly Resources Zero = new Resources();

    public static Resources operator +(Resources a, Resources b)
        => new Resources
        {
            Gold = a.Gold + b.Gold,
            Wood = a.Wood + b.Wood,
            Influence = a.Influence + b.Influence,
            Population = a.Population + b.Population
        };

    public static Resources operator -(Resources a, Resources b)
        => new Resources
        {
            Gold = a.Gold - b.Gold,
            Wood = a.Wood - b.Wood,
            Influence = a.Influence - b.Influence,
            Population = a.Population - b.Population
        };

    public bool CanAfford(Resources cost)
        => Gold >= cost.Gold && Wood >= cost.Wood &&
           Influence >= cost.Influence && Population >= cost.Population;

    public void ClampNonNegative()
    {
        if (Gold < 0) Gold = 0;
        if (Wood < 0) Wood = 0;
        if (Influence < 0) Influence = 0;
        if (Population < 0) Population = 0;
    }
}
