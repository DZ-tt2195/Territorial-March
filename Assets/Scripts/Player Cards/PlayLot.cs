using System.Collections.Generic;
using UnityEngine;

public class PlayLot : PlayerCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void ResolveCard(Player player, int logged)
    {
        base.ResolveCard(player, logged);
        if (CalculateActionsLost(player) >= dataFile.miscAmount)
        {
            DrawCard(player, logged);
            AddAction(player, logged);
        }
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = dataFile.startingCoin;
            if (CalculateActionsLost(player) + 1 >= dataFile.miscAmount)
                mathResult += 3 * dataFile.cardAmount + 3 * dataFile.actionAmount;
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
                actionsLost -= (int)stepParameters[2];
        }
        return actionsLost;
    }
}
