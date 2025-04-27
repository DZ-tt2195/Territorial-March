using System;
using UnityEngine;

public class Balance : AreaCard
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
        {
            if (player.areasControlled[1])
                player.DrawCardRPC(GetFile().cardAmount, logged);
            if (player.areasControlled[2])
                player.DrawCardRPC(GetFile().actionAmount, logged);
        }
    }
}
