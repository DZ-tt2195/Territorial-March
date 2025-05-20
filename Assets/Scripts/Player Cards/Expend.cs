using System.Collections.Generic;
using UnityEngine;

public class Expend : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        player.ResourceRPC(Resource.Coin, CalculateActionsLost(player) * dataFile.coinAmount, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = dataFile.startingCoin;
            mathResult += (CalculateActionsLost(player) + 1) * dataFile.coinAmount;
        }
        recalculate = false;
    }

    int CalculateActionsLost(Player player)
    {
        int actionsLost = 0;
        List<NextStep> listOfSteps = Log.inst.SearchHistory("ChangeResource");

        foreach (NextStep step in listOfSteps)
        {
            (string instruction, object[] stepParameters) = step.source.TranslateFunction(step.action);
            if ((int)stepParameters[1] == (int)Resource.Action && (int)stepParameters[2] < 0)
                actionsLost += Mathf.Abs((int)stepParameters[2]);
        }
        return actionsLost;
    }
}
