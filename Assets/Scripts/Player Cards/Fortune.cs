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
        player.ResourceRPC(Resource.Coin, DoMath(player), logged);
    }

    public override int DoMath(Player player)
    {
        int answer = this.dataFile.startingCoin;
        List<NextStep> listOfSteps = Log.inst.SearchHistory("ChangeResource");

        foreach (NextStep step in listOfSteps)
        {
            (string instruction, object[] stepParameters) = step.source.TranslateFunction(step.action);
            if ((int)stepParameters[1] == (int)Resource.Coin)
            {
                if ((int)stepParameters[2] > 0)
                    answer += (int)stepParameters[2];
            }
        }

        return answer;
    }
}
