using System.Collections.Generic;
using UnityEngine;

public class Swamp : AreaCard
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
            SetAllStats(player, GetFile(), player.CalcTroopScout(3).Item1, logged);
            LoseCoin(player, logged);
        }
    }
}
