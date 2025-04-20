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
        (int troop, int scout) = player.CalcTroopScout(3);
        SetAllStats(player, dataFile, troop, logged);
        DiscardCard(player, logged);
    }
}
