using System.Collections.Generic;
using UnityEngine;

public class AdvanceLots : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        AdvanceTroop(player, logged);
    }

    protected override void PostAdvance(Player player, bool success, int logged)
    {
        if (success)
            player.ResourceRPC(Resource.Coin, CalculateTroopsMoved(player) * dataFile.coinAmount, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = dataFile.startingCoin + dataFile.troopAmount*4;
            mathResult += (CalculateTroopsMoved(player) + 1) * dataFile.coinAmount;
        }
        recalculate = false;
    }

    int CalculateTroopsMoved(Player player)
    {
        int troopsMoved = 0;
        List<NextStep> listOfSteps = Log.inst.SearchHistory("MoveTroop");

        foreach (NextStep step in listOfSteps)
        {
            (string instruction, object[] stepParameters) = step.source.TranslateFunction(step.action);
            if ((int)stepParameters[3] >= 0)
                troopsMoved++;
        }
        return troopsMoved;
    }
}
