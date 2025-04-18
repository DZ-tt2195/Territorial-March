using UnityEngine;
using System.Collections.Generic;

public class Penalty : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        (int amount, List<int> list) = CanRetreat(player);
        if (amount > 0)
        {
            if (player.resourceDict[Resource.Coin] >= dataFile.coinAmount)
                AskRetreatTroop(player, logged);
            else
                RetreatTroop(player, logged);
        }
    }

    protected override void PostRetreat(Player player, bool success, int logged)
    {
        if (!success)
            player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
    }
}
