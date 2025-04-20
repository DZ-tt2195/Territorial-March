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
        AskLoseAction(player, logged);
    }

    protected override void PostPayment(Player player, bool success, int logged)
    {
        player.ResourceRPC(Resource.Coin, mathResult, logged);
    }

    public override void DoMath(Player player)
    {
        if (recalculate)
        {
            mathResult = 0;
            if (player.resourceDict[Resource.Action] >= 1)
            {
                mathResult = -3;
                List<NextStep> listOfSteps = Log.inst.SearchHistory("ChangeResource");

                foreach (NextStep step in listOfSteps)
                {
                    (string instruction, object[] stepParameters) = step.source.TranslateFunction(step.action);
                    if ((int)stepParameters[1] == (int)Resource.Coin)
                    {
                        if ((int)stepParameters[2] > 0)
                            mathResult += (int)stepParameters[2];
                    }
                }
            }
        }
        recalculate = false;
    }
}
