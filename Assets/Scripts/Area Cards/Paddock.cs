using System.Collections.Generic;
using UnityEngine;

public class Paddock : AreaCard
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
            if (player.resourceDict[Resource.Action] > player.cardsInHand.Count)
                AdvanceTroop(player, logged);
        }
    }
}
