using UnityEngine;
using System.Collections.Generic;

public class Desert : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        if (!player.BoolFromAbilities(true, nameof(IgnoreArea), IgnoreArea.CheckParameters(), logged))
            AskLoseScout(player, logged);
    }

    protected override void PostLoseScout(Player player, bool success, int logged)
    {
        if (!success)
            player.ResourceRPC(Resource.Coin, -1 * dataFile.coinAmount, logged);
    }
}
