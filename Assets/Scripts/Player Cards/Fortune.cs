using System.Collections.Generic;
using UnityEngine;

public class Fortune : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AskDiscardCard(player, logged);
    }

    protected override void PostDiscard(Player player, bool success, int logged)
    {
        if (success)
            player.ResourceRPC(Resource.Coin, CalculateCoinsGained(player), logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
            mathResult = -3 + CalculateCoinsGained(player);
        recalculate = false;
    }

    int CalculateCoinsGained(Player player)
    {
        int coinGained = 0;
        List<NextStep> listOfSteps = Log.inst.SearchHistory("ChangeResource");

        foreach (NextStep step in listOfSteps)
        {
            (string instruction, object[] stepParameters) = step.source.TranslateFunction(step.action);
            if ((int)stepParameters[1] == (int)Resource.Coin && (int)stepParameters[2] > 0)
                coinGained += (int)stepParameters[2];
        }
        return coinGained;
    }
}
