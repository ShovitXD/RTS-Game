using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    private GameManager gameManager; // no serialized ref

    void Awake() { gameManager = GameManager.Instance; }
    void OnEnable() { if (!gameManager) gameManager = GameManager.Instance; }

    // Hooked by the button (see WireNextTurn below)
    public void NextTurn()
    {
        if (!gameManager) { Debug.LogError("TurnSystem: no GameManager."); return; }

        gameManager.EndTurn();

        var income = new Resources[GameManager.KingdomCount];
        var popDelta = new int[GameManager.KingdomCount];

        foreach (var cell in TileRegistry.AllCells)
        {
            if (!cell || cell.Owner == Kingdom.None || cell.Type == null) continue;
            int ki = (int)cell.Owner;

            var v = cell.Values; v.Population = 0;
            income[ki] += v;

            int added = cell.GrowPopulationOneTurn();
            if (added > 0) popDelta[ki] += added;
        }

        for (int i = 0; i < GameManager.KingdomCount; i++)
        {
            var k = (Kingdom)i;
            if (income[i].Gold != 0 || income[i].Wood != 0 || income[i].Influence != 0)
                gameManager.AddTo(k, income[i]);
            if (popDelta[i] > 0)
                gameManager.AddTo(k, new Resources { Population = popDelta[i] });
        }

        gameManager.BeginTurn();
    }
}
