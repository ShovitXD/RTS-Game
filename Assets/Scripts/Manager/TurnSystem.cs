using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    [SerializeField] private GameManager gameManager; // assign in Inspector

    // Hook this to your Next Turn button (OnClick)
    public void NextTurn()
    {
        if (!gameManager) { Debug.LogError("TurnSystem: Assign GameManager in the Inspector."); return; }

        // Close out previous turn
        gameManager.EndTurn();

        var income = new Resources[GameManager.KingdomCount]; // Gold/Wood/Influence totals
        var popIncome = new int[GameManager.KingdomCount];    // Population deltas from growth

        foreach (var cell in TileRegistry.AllCells)
        {
            if (!cell || cell.Owner == Kingdom.None || cell.Type == null) continue;
            int ki = (int)cell.Owner;

            // 1) Static resource yields from the tile (no population here)
            var v = cell.Values; v.Population = 0;
            income[ki] += v;

            // 2) Population growth (per-tile) → roll up to global per-kingdom population
            int added = cell.GrowPopulationOneTurn();
            if (added > 0) popIncome[ki] += added;
        }

        // Apply resources + population growth to each kingdom wallet
        for (int i = 0; i < GameManager.KingdomCount; i++)
        {
            var k = (Kingdom)i;

            if (income[i].Gold != 0 || income[i].Wood != 0 || income[i].Influence != 0)
                gameManager.AddTo(k, income[i]);

            if (popIncome[i] > 0)
                gameManager.AddTo(k, new Resources { Population = popIncome[i] });
        }

        // Begin new turn
        gameManager.BeginTurn();
    }
}
