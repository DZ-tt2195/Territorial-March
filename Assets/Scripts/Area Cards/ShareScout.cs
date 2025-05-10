using System.Collections.Generic;
using UnityEngine;

public class ShareScout : AreaCard
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
            if (player.areasControlled[areaNumber])
            {
                for (int i = 0; i < 4; i++)
                {
                    if (i != this.areaNumber)
                        player.ChangeScoutRPC(i, dataFile.scoutAmount, logged);
                    else
                        player.ChangeScoutRPC(i, -1* dataFile.scoutAmount, logged);
                }
            }
        }
    }
}
