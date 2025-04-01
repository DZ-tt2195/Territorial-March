using System.Collections.Generic;
using UnityEngine;

public class Progress : AreaCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
    }

    public override void AreaInstructions(Player player, int logged)
    {
        base.AreaInstructions(player, logged);
        AdvanceTroop(player, GetFile(), logged);
    }

    protected override (int, List<int>) CanAdvance(Player player)
    {
        (int troop, int scout) = player.CalcTroopScout(0);
        if (troop == 0)
            return (0, new List<int>() { -1 });
        else
            return (troop, new List<int>() { 0 });
    }
}
