using System.Collections.Generic;
using UnityEngine;

public class Duo : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        AskLoseCoin(player, logged);
    }

    protected override void PostPayment(Player player, bool success, int logged)
    {
        if (success)
        {
            if (player.CalcTroopScout(1).Item1 > 0)
                player.MoveTroopRPC(1, 3, logged);
            if (player.CalcTroopScout(2).Item1 > 0)
                player.MoveTroopRPC(2, 3, logged);
        }
    }
}
